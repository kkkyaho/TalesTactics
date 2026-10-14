using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class PixelBattlefield
    {
        Material meadow;
        Texture2D trees;
        readonly Sprite[] canopySprites=new Sprite[4];
        readonly List<SpriteRenderer> canopy=new List<SpriteRenderer>();
        readonly TerrainGeometry exterior=new TerrainGeometry();
        Vector3 playableCenter;float playableRadius;
        bool Verdant=>stage==0||stage==1||stage==4;
        public int CanopyCount=>canopy.Count;
        void InitializeLivingTerrain()
        {
            trees=Resources.Load<Texture2D>("TalesTactics/Ornate/Trees");
            var grass=Resources.Load<Texture2D>("TalesTactics/Ornate/Ground");
            meadow=new Material(surfaces[2]);owned.Add(meadow);meadow.name="Living forest floor";
            meadow.SetTexture("_MainTex",grass);meadow.SetVector("_AtlasRect",new Vector4(0,0,1,1));
            meadow.SetFloat("_WorldCliff",1);meadow.SetFloat("_DetailContrast",.9f);
            floorMaterial.SetTexture("_MeadowTex",grass);floorMaterial.SetFloat("_LivingFloor",Verdant?1:0);
            foreach(var material in surfaces){material.SetFloat("_SurfaceRelief",.25f);material.SetFloat("_Dapple",Verdant?.12f:.025f);}
            floorMaterial.SetFloat("_SurfaceRelief",.35f);floorMaterial.SetFloat("_Dapple",Verdant?.17f:.035f);
        }
        static float DetailNoise(int x,int z,int salt=0)
        {unchecked{uint h=(uint)(x*73856093^z*19349663^salt*83492791);h^=h>>13;h*=1274126177;return (h&65535)/65535f;}}
        void DressLivingTile(GridTile tile,GridMap map,float step)
        {
            if(tile.Terrain==TerrainType.Water)return;
            int x=tile.Coordinate.x,z=tile.Coordinate.y;var p=tile.WorldPosition(step);
            if(!tile.Walkable&&Verdant)
            {
                for(int k=0;k<3;k++)Foliage(k==0?9:2,p+new Vector3((k-1)*.27f,.015f,.22f),.28f+k*.055f);
            }
            // Low edge foliage does not cover the tile center or change collision/navigation.
            foreach(var d in Directions)
            {
                var neighbor=map[tile.Coordinate+d];
                bool edge=neighbor==null||!neighbor.Walkable||neighbor.Terrain==TerrainType.Water;
                if(!edge||DetailNoise(x+d.x,z+d.y,3)<.34f)continue;
                var foot=p+new Vector3(d.x,0,d.y)*.43f+Vector3.up*.02f;
                Foliage(Verdant?2:stage==2?4:15,foot,Verdant?.22f:stage==2?.18f:.12f);
                if(Verdant&&neighbor==null)Foliage(0,foot-Vector3.up*.5f,.7f);
            }
        }
        void LivingExterior(GridMap map,float step,int maxX,int maxZ)
        {
            playableCenter=new Vector3(maxX*.5f,0,maxZ*.5f);playableRadius=Mathf.Max(maxX,maxZ)*.5f;
            // A broken, irregular skirt joins the playable architecture to real world-space scenery.
            // It stays below the walkable surface and has no colliders.
            // Continuous irregular perimeter, not separate floating square grass tiles.
            var inner=new List<Vector3>();var outer=new List<Vector3>();
            for(int side=0;side<4;side++)
            {
                int segments=(side%2==0?maxX:maxZ)+2;
                for(int s=0;s<segments;s++)
                {
                    float t=(float)s/segments;float x=side==0?Mathf.Lerp(-.5f,maxX+.5f,t):side==1?maxX+.5f:side==2?Mathf.Lerp(maxX+.5f,-.5f,t):-.5f;
                    float z=side==0?-.5f:side==1?Mathf.Lerp(-.5f,maxZ+.5f,t):side==2?maxZ+.5f:Mathf.Lerp(maxZ+.5f,-.5f,t);
                    var near=map[new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(x),0,maxX),Mathf.Clamp(Mathf.RoundToInt(z),0,maxZ))];if(near==null)continue;
                    float noise=DetailNoise(s,side,7);float h=near.WorldPosition(step).y-.28f;
                    var a=new Vector3(x,h,z);var away=(new Vector3(x,0,z)-playableCenter).normalized;var b=a+away*(2.1f+noise*1.7f)-Vector3.up*(.9f+noise*.3f);
                    inner.Add(a);outer.Add(b);
                    if(Verdant)
                    {
                        Tree(3,Vector3.Lerp(a,b,.7f),.7f+noise*.5f);
                        if(s%2==0)Tree((s+side)%3,b-away*.4f,2.7f+noise*1.6f);
                        Foliage(9,Vector3.Lerp(a,b,.3f),.38f+noise*.25f);
                        Foliage(0,b-Vector3.up*.45f,1.1f);
                    }
                    else if(s%2==0)geometry[stage==2?6:wall].Rock(b-Vector3.up*.5f,.65f+noise*.4f,.9f+noise*.5f,s+side*19);
                }
            }
            for(int i=0;i<inner.Count;i++)
            {
                int next=(i+1)%inner.Count;
                if(Verdant)exterior.Quad(inner[i],inner[next],outer[next],outer[i]);
                else geometry[stage==2?6:wall].Quad(inner[i],inner[next],outer[next],outer[i]);
                // Deep world-space cliff faces hide the underside and connect the skirt to the ravine.
                var materialIndex=stage==2?6:wall;
                geometry[materialIndex].Quad(outer[i]-Vector3.up*3,outer[i],outer[next],outer[next]-Vector3.up*3,.85f,new Vector2(2,3));
                if(Verdant&&i%2==0)Foliage(0,outer[i]-Vector3.up*1.3f,1.8f);
            }
            if(Verdant){var mesh=exterior.Flush(transform,"Continuous forest surroundings",meadow);if(mesh!=null)meshes.Add(mesh);}
        }
        void Tree(int index,Vector3 foot,float height)
        {
            if(trees==null)return;
            if(canopySprites[index]==null)
            {
                // The atlas has a taller upper row. Use measured row bounds rather than cutting roots.
                float w=trees.width*.5f,h=trees.height*(index<2?.54f:.46f),y=index<2?trees.height*.46f:0;
                var art=Sprite.Create(trees,new Rect(index%2*w,y,w,h),new Vector2(.5f,.05f),100,0,SpriteMeshType.FullRect);sprites.Add(art);canopySprites[index]=art;
            }
            var sprite=canopySprites[index];var go=new GameObject("Forest canopy "+index,typeof(SpriteRenderer));go.transform.SetParent(transform,false);
            go.transform.localPosition=foot;go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);go.transform.rotation=view.transform.rotation;
            var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=spriteMaterial;renderer.color=Color.white;billboards.Add(go.transform);canopy.Add(renderer);
            if(index<3&&shadowCasters.Count<24)shadowCasters.Add(new Vector4(foot.x,foot.z,height,foot.y));
        }
        void UpdateLivingCanopy()
        {
            // Foreground crowns fade when they could obscure the playable board after a rotation.
            var direction=view.transform.forward;direction.y=0;direction.Normalize();
            foreach(var renderer in canopy)
            {
                var delta=renderer.transform.position-playableCenter;float depth=Vector3.Dot(delta,direction);
                float alpha=depth < -playableRadius*.50f&&renderer.name!="Forest canopy 3"?0:1;
                var c=renderer.color;if(!Mathf.Approximately(c.a,alpha)){c.a=alpha;renderer.color=c;}
            }
        }
    }
}
