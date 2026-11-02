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
            floorMaterial.SetFloat("_SurfaceRelief",.22f);floorMaterial.SetFloat("_Dapple",Verdant?.11f:.025f);
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
                float noise=DetailNoise(x,z,17+d.x*3+d.y*7);
                if(!edge||noise<.38f)continue;
                var outward=new Vector3(d.x,0,d.y);var tangent=Vector3.Cross(outward,Vector3.up);
                var foot=p+outward*.45f+tangent*((noise-.5f)*.44f)+Vector3.up*.015f;
                Foliage(Verdant?(noise>.74f?9:2):stage==2?4:15,foot,(Verdant?.18f:stage==2?.15f:.10f)*(1+noise*.32f));
                if(Verdant&&neighbor==null)Foliage(0,foot-Vector3.up*.5f,.7f);
            }
        }
        void LivingExterior(GridMap map,float step,int maxX,int maxZ)
        {
            playableCenter=new Vector3(maxX*.5f,0,maxZ*.5f);playableRadius=Mathf.Max(maxX,maxZ)*.5f;
            // A broken, irregular skirt joins the playable architecture to real world-space scenery.
            // It stays below the walkable surface and has no colliders.
            // Continuous irregular perimeter, not separate floating square grass tiles.
            var inner=new List<Vector3>();var outer=new List<Vector3>();var exposed=new List<bool>();var tallTrees=new List<Vector4>();
            for(int side=0;side<4;side++)
            {
                int segments=(side%2==0?maxX:maxZ)+2;
                // Every side has an opening, so camera rotation never reveals another uniform fence.
                float opening=segments*(.30f+DetailNoise(side,stage,29)*.36f);
                // Two separated exposures per side break the flower band without filling the board.
                int rockA=Mathf.FloorToInt(segments*(.14f+DetailNoise(side,stage,59)*.16f));
                int rockB=Mathf.FloorToInt(segments*(.62f+DetailNoise(side,stage,61)*.16f));
                for(int s=0;s<segments;s++)
                {
                    float t=(float)s/segments;float x=side==0?Mathf.Lerp(-.5f,maxX+.5f,t):side==1?maxX+.5f:side==2?Mathf.Lerp(maxX+.5f,-.5f,t):-.5f;
                    float z=side==0?-.5f:side==1?Mathf.Lerp(-.5f,maxZ+.5f,t):side==2?maxZ+.5f:Mathf.Lerp(maxZ+.5f,-.5f,t);
                    var near=map[new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(x),0,maxX),Mathf.Clamp(Mathf.RoundToInt(z),0,maxZ))];if(near==null)continue;
                    float noise=DetailNoise(s,side,7);float h=near.WorldPosition(step).y-.28f;
                    float cluster=Mathf.Lerp(DetailNoise(s/3,side,23+stage),DetailNoise(s/3+1,side,23+stage),(s%3)/3f);
                    var a=new Vector3(x,h,z);var away=(new Vector3(x,0,z)-playableCenter).normalized;
                    var profile=ExteriorLandform(side,t,noise);
                    var b=a+away*profile.x-Vector3.up*profile.y;
                    // Broad outcrops and narrow gullies break the green belt into separate patches.
                    // The rectangle of playable tiles remains exact; only scenery outside it moves.
                    bool rockPatch=Verdant&&(s==rockA||s==rockB||profile.x<1.0f||profile.z>.42f);
                    inner.Add(a);outer.Add(b);exposed.Add(rockPatch);
                    if(Verdant)
                    {
                        if(rockPatch)
                        {
                            var outward=side==0?Vector3.back:side==1?Vector3.right:side==2?Vector3.forward:Vector3.left;
                            var rock=Vector3.Lerp(a,b,.66f)+outward*.12f-Vector3.up*.28f;
                            // Rock's authored radial variation is at most 1.03. Keep every vertex
                            // outside the grid boundary, even on the narrowest corner of the skirt.
                            float radius=Mathf.Min(.42f+noise*.12f,(Vector3.Dot(rock-a,outward)-.10f)/1.05f);
                            geometry[6].Rock(rock,radius,.58f+noise*.16f,side*37+s);
                        }
                        if(!rockPatch&&(s%3==0||noise>.55f))Tree(3,Vector3.Lerp(a,b,.68f),.60f+cluster*.38f,noise>.5f);
                        float height=2.25f+cluster*.65f+noise*.35f;
                        var foot=b-away*.22f;
                        if(!rockPatch&&profile.x>1.35f&&Mathf.Abs(s-opening)>1.55f&&noise>.26f&&CanPlaceCanopy(tallTrees,foot,height))
                        {
                            int species=Mathf.Min(2,(int)(DetailNoise(s,side,37)*3));
                            Tree(species,foot,height,DetailNoise(s,side,39)>.5f);
                            tallTrees.Add(new Vector4(foot.x,foot.z,height,0));
                        }
                        if(!rockPatch&&noise>.35f)Foliage(9,Vector3.Lerp(a,b,.3f),.27f+cluster*.18f);
                        if(!rockPatch&&noise>.55f)Foliage(0,b-Vector3.up*.45f,.85f+noise*.2f);
                    }
                    else if(profile.x>1.25f&&noise>.54f)
                    {
                        // Sparse fallen blocks, never a regular row of tall cylindrical battlements.
                        float radius=.38f+noise*.32f;
                        geometry[stage==2?6:wall].Rock(b-Vector3.up*.5f,radius,.40f+noise*.15f,s+side*19);
                    }
                }
            }
            for(int i=0;i<inner.Count;i++)
            {
                int next=(i+1)%inner.Count;
                if(Verdant&&!exposed[i])exterior.Quad(inner[i],inner[next],outer[next],outer[i]);
                else if(Verdant)geometry[6].Quad(inner[i],inner[next],outer[next],outer[i]);
                else geometry[stage==2?6:wall].Quad(inner[i],inner[next],outer[next],outer[i]);
                // Connected uneven shelves replace the constant-depth vertical curtain. Their
                // shared endpoint profile keeps corners closed without changing the playable grid.
                DressExteriorCliff(outer[i],outer[next]);
                if(Verdant&&!exposed[i]&&DetailNoise(i,stage,43)>.58f)Foliage(0,outer[i]-Vector3.up*1.15f,1.3f+DetailNoise(i,stage,47)*.3f);
            }
            if(Verdant){var mesh=exterior.Flush(transform,"Continuous forest surroundings",meadow);if(mesh!=null)meshes.Add(mesh);}
        }
        static bool CanPlaceCanopy(List<Vector4> placed,Vector3 foot,float height)
        {
            foreach(var tree in placed)
            {
                float clearance=(tree.z+height)*.40f;
                float dx=foot.x-tree.x,dz=foot.z-tree.y;
                if(dx*dx+dz*dz<clearance*clearance)return false;
            }
            return true;
        }
        void Tree(int index,Vector3 foot,float height,bool mirrored=false)
        {
            if(trees==null)return;
            if(canopySprites[index]==null)
            {
                // Measured SD atlas islands, including transparent padding around leaves and roots.
                // The oak extends past the old half-column boundary; equal cells would cut its crown.
                Rect[] islands={new Rect(40,580,610,630),new Rect(690,570,530,640),new Rect(90,30,490,550),new Rect(635,25,600,415)};
                var region=islands[index];float sx=trees.width/1254f,sy=trees.height/1254f;
                region=new Rect(region.x*sx,region.y*sy,region.width*sx,region.height*sy);
                var art=Sprite.Create(trees,region,new Vector2(.5f,.03f),100,0,SpriteMeshType.FullRect);sprites.Add(art);canopySprites[index]=art;
            }
            var sprite=canopySprites[index];var go=new GameObject("Forest canopy "+index,typeof(SpriteRenderer));go.transform.SetParent(transform,false);
            go.transform.localPosition=foot;go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);go.transform.rotation=view.transform.rotation;
            var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=spriteMaterial;renderer.color=Color.white;renderer.flipX=mirrored;StandingSpriteDepth.Configure(renderer);billboards.Add(go.transform);canopy.Add(renderer);
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
