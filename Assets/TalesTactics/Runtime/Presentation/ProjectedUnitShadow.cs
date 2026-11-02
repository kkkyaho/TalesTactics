using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    // A short, world-lit silhouette. Its triangles are clipped to actual dry receiving tiles.
    // The caller updates after authored animation, so equipment, sprite frames and motion agree.
    public sealed class ProjectedUnitShadow:MonoBehaviour
    {
        const float ProjectionScale=.38f,SurfaceOffset=.010f,TileRadius=.499f;
        const int CachedSprites=128;
        sealed class SpriteGeometry
        {
            public readonly Vector2[] Vertices,Uvs;public readonly ushort[] Triangles;
            public SpriteGeometry(Sprite sprite){Vertices=sprite.vertices;Uvs=sprite.uv;Triangles=sprite.triangles;}
        }
        struct Vertex
        {
            public Vector3 Position;public Vector2 Uv;public float Alpha;
            public Vertex(Vector3 position,Vector2 uv,float alpha){Position=position;Uv=uv;Alpha=alpha;}
            public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex(Vector3.LerpUnclamped(a.Position,b.Position,t),Vector2.LerpUnclamped(a.Uv,b.Uv,t),Mathf.LerpUnclamped(a.Alpha,b.Alpha,t));
        }
        readonly Dictionary<Sprite,SpriteGeometry> cache=new Dictionary<Sprite,SpriteGeometry>(CachedSprites);
        readonly Queue<Sprite> cacheOrder=new Queue<Sprite>(CachedSprites);
        readonly List<Vector3> vertices=new List<Vector3>(128);
        readonly List<Vector2> uvs=new List<Vector2>(128);
        readonly List<Color> colors=new List<Color>(128);
        readonly List<int> triangles=new List<int>(192);
        readonly Vertex[] clipA=new Vertex[12],clipB=new Vertex[12];
        Vertex[] projected=new Vertex[4];
        SpriteRenderer actor;GridMap map;float step;int theme;
        Mesh mesh;MeshRenderer surface;Material material;MaterialPropertyBlock properties;bool ownsMaterial;
        public Sprite SourceSprite=>actor!=null?actor.sprite:null;
        public Vector3 GroundAnchor{get;private set;}
        public float ReceiverHeight{get;private set;}
        public float Opacity{get;private set;}
        public bool Visible=>surface!=null&&surface.enabled&&surface.gameObject.activeInHierarchy;
        public void Initialize(SpriteRenderer source,GridMap receivers,float heightStep,int chapterTheme,Material shared=null)
        {
            actor=source;map=receivers;step=heightStep;theme=chapterTheme;
            mesh=new Mesh{name="Projected unit silhouette"};mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            // Keep every tactical overlay and the denser foot contact above the body shadow.
            surface=gameObject.AddComponent<MeshRenderer>();surface.sortingOrder=-5;
            surface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;surface.receiveShadows=false;
            ownsMaterial=shared==null;
            material=shared!=null?shared:new Material(Resources.Load<Shader>("TalesTactics/ProjectedUnitShadow")){name="Projected unit shadow"};
            surface.sharedMaterial=material;surface.enabled=false;properties=new MaterialPropertyBlock();
        }
        public void UpdateProjection(Vector3 groundAnchor,float lift,bool visible)
        {
            GroundAnchor=groundAnchor;Opacity=0;
            if(surface==null)return;
            surface.enabled=false;
            if(!isActiveAndEnabled||!visible||actor==null||!actor.enabled||actor.forceRenderingOff||!actor.gameObject.activeInHierarchy||actor.sprite==null||map==null)return;
            var cell=new Vector2Int(Mathf.FloorToInt(groundAnchor.x+.5f),Mathf.FloorToInt(groundAnchor.z+.5f));
            var receiver=map[cell];
            if(receiver==null||receiver.Terrain==TerrainType.Water)return;
            ReceiverHeight=receiver.Height*step;
            // During ascent the feet must reach the upper surface before it can receive shade.
            if(ReceiverHeight>groundAnchor.y+.025f)return;
            float elevation=Mathf.Max(0,lift)+Mathf.Max(0,groundAnchor.y-ReceiverHeight);
            Opacity=.22f*actor.color.a*Mathf.Lerp(1,.48f,Mathf.Clamp01(elevation*1.5f));
            if(Opacity<.001f)return;
            var sprite=actor.sprite;
            if(!cache.TryGetValue(sprite,out var geometry))
            {
                if(cache.Count>=CachedSprites)cache.Remove(cacheOrder.Dequeue());
                geometry=new SpriteGeometry(sprite);cache.Add(sprite,geometry);cacheOrder.Enqueue(sprite);
            }
            if(projected.Length<geometry.Vertices.Length)projected=new Vertex[Mathf.NextPowerOfTwo(geometry.Vertices.Length)];
            var world=actor.transform.localToWorldMatrix;
            // Recover the upright billboard axes from its normal, retaining the authored roll.
            // Removing camera pitch here prevents a standing sprite from painting its lean on the floor.
            var forward=actor.transform.forward;
            var right=Vector3.Cross(Vector3.up,forward);
            if(right.sqrMagnitude<.0001f)right=Vector3.right;else right.Normalize();
            var up=Vector3.Cross(forward,right).normalized;
            var direction=BattleEnvironmentPalette.ShadowDirection;
            // A billboard has no thickness. Preserve a modest body width when the camera's
            // right axis aligns with the light, instead of collapsing the silhouette to a line.
            // Every vertex shares this width axis; there is no height-dependent sideways warp.
            var transverse=new Vector3(direction.z,0,-direction.x);
            float across=Vector3.Dot(right,transverse);
            var widthAxis=right+transverse*(across<0?-1:1)*Mathf.Max(0,.55f-Mathf.Abs(across));
            for(int i=0;i<geometry.Vertices.Length;i++)
            {
                var vertex=geometry.Vertices[i];if(actor.flipX)vertex.x=-vertex.x;if(actor.flipY)vertex.y=-vertex.y;
                var offset=world.MultiplyVector(new Vector3(vertex.x,vertex.y,0));
                float height=Mathf.Max(0,Vector3.Dot(offset,up));
                var position=groundAnchor+widthAxis*Vector3.Dot(offset,right)*.88f+direction*((height+elevation)*ProjectionScale);
                position.y=ReceiverHeight+SurfaceOffset;
                projected[i]=new Vertex(position,geometry.Uvs[i],Mathf.Lerp(1,.76f,Mathf.Clamp01(height/1.5f)));
            }
            vertices.Clear();uvs.Clear();colors.Clear();triangles.Clear();
            for(int i=0;i<geometry.Triangles.Length;i+=3)
            {
                var a=projected[geometry.Triangles[i]];var b=projected[geometry.Triangles[i+1]];var c=projected[geometry.Triangles[i+2]];
                int minX=Mathf.FloorToInt(Mathf.Min(a.Position.x,Mathf.Min(b.Position.x,c.Position.x))+.5f);
                int maxX=Mathf.FloorToInt(Mathf.Max(a.Position.x,Mathf.Max(b.Position.x,c.Position.x))+.5f);
                int minZ=Mathf.FloorToInt(Mathf.Min(a.Position.z,Mathf.Min(b.Position.z,c.Position.z))+.5f);
                int maxZ=Mathf.FloorToInt(Mathf.Max(a.Position.z,Mathf.Max(b.Position.z,c.Position.z))+.5f);
                for(int x=minX;x<=maxX;x++)for(int z=minZ;z<=maxZ;z++)
                {
                    var tile=map[new Vector2Int(x,z)];
                    if(tile==null||tile.Terrain==TerrainType.Water||Mathf.Abs(tile.Height*step-ReceiverHeight)>.005f)continue;
                    clipA[0]=a;clipA[1]=b;clipA[2]=c;
                    int count=Clip(clipA,3,clipB,0,x-TileRadius,true);
                    count=Clip(clipB,count,clipA,0,x+TileRadius,false);
                    count=Clip(clipA,count,clipB,2,z-TileRadius,true);
                    count=Clip(clipB,count,clipA,2,z+TileRadius,false);
                    AppendPolygon(clipA,count);
                }
            }
            mesh.Clear();
            if(vertices.Count==0)return;
            mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var tint=BattleEnvironmentPalette.Shadow(theme);tint.a=Opacity;
            properties.SetTexture("_MainTex",sprite.texture);properties.SetColor("_Color",tint);surface.SetPropertyBlock(properties);
            surface.enabled=true;
        }
        static int Clip(Vertex[] source,int count,Vertex[] destination,int axis,float edge,bool greater)
        {
            if(count==0)return 0;
            int written=0;var previous=source[count-1];float previousDistance=previous.Position[axis]-edge;
            bool previousInside=greater?previousDistance>=0:previousDistance<=0;
            for(int i=0;i<count;i++)
            {
                var current=source[i];float distance=current.Position[axis]-edge;bool inside=greater?distance>=0:distance<=0;
                if(inside!=previousInside)destination[written++]=Vertex.Lerp(previous,current,previousDistance/(previousDistance-distance));
                if(inside)destination[written++]=current;
                previous=current;previousDistance=distance;previousInside=inside;
            }
            return written;
        }
        void AppendPolygon(Vertex[] polygon,int count)
        {
            if(count<3)return;
            int start=vertices.Count;
            for(int i=0;i<count;i++)
            {
                // Geometry is constructed in world space, but remains under its owning unit.
                vertices.Add(transform.InverseTransformPoint(polygon[i].Position));uvs.Add(polygon[i].Uv);
                colors.Add(new Color(1,1,1,polygon[i].Alpha));
            }
            for(int i=1;i<count-1;i++){triangles.Add(start);triangles.Add(start+i);triangles.Add(start+i+1);}
        }
        void OnDisable(){if(surface!=null)surface.enabled=false;}
        void OnDestroy()
        {
            if(mesh!=null)Destroy(mesh);if(ownsMaterial&&material!=null)Destroy(material);
            cache.Clear();cacheOrder.Clear();
        }
    }
}
