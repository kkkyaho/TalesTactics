using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TalesTactics.PlayModeTests
{
    // Render the production projection and shader. Transparent negative controls ensure that
    // geometry alone, a filled billboard, or a stale sprite atlas cannot satisfy these checks.
    public sealed class CharacterDepthTests
    {
        const int Size=256;
        const float Extent=3;
        readonly List<Object> owned=new List<Object>();
        SpriteRenderer actor;ProjectedUnitShadow shadow;Sprite hollow,solid,otherFrame;
        Matrix4x4 View=>Matrix4x4.Rotate(Quaternion.Euler(90,0,0)).inverse;
        Matrix4x4 Projection=>GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-Extent,Extent,-Extent,Extent,-10,10),true);

        [UnitySetUp] public IEnumerator CreateProjectionFixture()
        {
            var atlas=Own(new Texture2D(128,64,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp});
            var pixels=new Color32[128*64];
            for(int y=0;y<64;y++)for(int x=0;x<128;x++)
            {
                bool body=x<64&&x>=12&&x<52&&y>=8&&y<58;
                bool hole=x>=24&&x<40&&y>=27&&y<44;
                bool foot=x>=4&&x<28&&y<24;
                bool second=x>=99&&x<116&&y>=5&&y<60;
                pixels[y*128+x]=new Color32(255,255,255,(byte)((body&&!hole||foot||second)?255:0));
            }
            atlas.SetPixels32(pixels);atlas.Apply();
            hollow=Own(Sprite.Create(atlas,new Rect(0,0,64,64),new Vector2(.5f,0),32,0,SpriteMeshType.FullRect));
            otherFrame=Own(Sprite.Create(atlas,new Rect(64,0,64,64),new Vector2(.5f,0),32,0,SpriteMeshType.FullRect));
            var opaque=Own(new Texture2D(64,64,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp});
            opaque.SetPixels(Enumerable.Repeat(Color.white,64*64).ToArray());opaque.Apply();
            solid=Own(Sprite.Create(opaque,new Rect(0,0,64,64),new Vector2(.5f,0),32,0,SpriteMeshType.FullRect));
            actor=Own(new GameObject("Depth alpha fixture actor")).AddComponent<SpriteRenderer>();actor.sprite=hollow;
            actor.transform.rotation=Quaternion.Euler(57,0,0);
            CreateShadow(FlatMap());yield return null;
        }
        [UnityTearDown] public IEnumerator DestroyProjectionFixture()
        {
            for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)Object.Destroy(owned[i]);
            owned.Clear();yield return null;yield return null;
        }
        T Own<T>(T value) where T:Object {owned.Add(value);return value;}
        static GridMap FlatMap()
        {
            var map=new GridMap();for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)
            {var p=new Vector2Int(x,z);map.Tiles.Add(p,new GridTile{Coordinate=p});}return map;
        }
        void CreateShadow(GridMap map)
        {
            if(shadow!=null)Object.DestroyImmediate(shadow.gameObject);
            shadow=Own(new GameObject("Depth alpha fixture projection")).AddComponent<ProjectedUnitShadow>();
            shadow.Initialize(actor,map,.5f,0);Project();
        }
        void Project(float lift=0,bool visible=true)=>shadow.UpdateProjection(new Vector3(actor.transform.position.x,0,actor.transform.position.z),lift,visible);

        [UnityTest] public IEnumerator ProjectionRendersAlphaHolesAndChangesWithAtlasFrameAndFlip()
        {
            var hollowImage=Render();AssertVisible(hollowImage,"authored alpha");
            // The exact same geometry and pivot, with an opaque source, is the negative control.
            var holePoint=WorldAtUv(new Vector2(32f/128,35f/64));
            actor.sprite=solid;Project();var filled=Render();AssertVisible(filled,"opaque negative control");
            Assert.That(SampleAlpha(filled,holePoint),Is.GreaterThan(4),"The hole probe must lie inside the projected body.");
            Assert.That(SampleAlpha(hollowImage,holePoint),Is.LessThan(2),"Transparent artwork holes must not become a rectangular body shadow.");
            Assert.That(Area(hollowImage),Is.LessThan(Area(filled)*.8f));
            actor.sprite=hollow;actor.flipX=true;Project();var flipped=Render();
            Assert.That(Difference(hollowImage,flipped),Is.GreaterThan(.06f),"An asymmetric authored silhouette must follow SpriteRenderer.flipX.");
            Assert.That(Area(flipped),Is.EqualTo(Area(hollowImage)).Within(Area(hollowImage)*.12f));
            actor.flipX=false;actor.sprite=otherFrame;Project();var changed=Render();
            AssertVisible(changed,"second atlas frame");
            Assert.That(Area(changed),Is.LessThan(Area(hollowImage)*.75f),"The shadow kept the prior sprite rect/UVs after an atlas frame change.");
            actor.sprite=hollow;Project();Assert.That(Difference(hollowImage,Render()),Is.LessThan(.005f),"Returning to a sprite must restore its silhouette.");
            yield return null;
        }

        [UnityTest] public IEnumerator ProjectionTracksActorScaleRollCameraYawAndLiftOnTheGround()
        {
            actor.sprite=solid;Project();var baseline=Render();AssertVisible(baseline,"resting");
            actor.transform.localScale=new Vector3(1.4f,1.2f,1);Project();var enlarged=Render();
            Assert.That(Area(enlarged),Is.GreaterThan(Area(baseline)*1.3f),"A larger visible actor needs a correspondingly larger body silhouette.");
            actor.transform.localScale=Vector3.one;actor.transform.rotation=Quaternion.Euler(57,0,18);Project();var rolled=Render();
            Assert.That(Difference(baseline,rolled),Is.GreaterThan(.05f),"Authored recoil roll was lost by the ground projection.");
            actor.transform.rotation=Quaternion.Euler(57,90,0);Project();var rotated=Render();
            Assert.That(Difference(baseline,rotated),Is.GreaterThan(.1f),"Camera-facing width must be reprojected when the camera changes quadrant.");
            // The production camera starts near -39 degrees: its billboard width nearly
            // coincides with the sunlight direction, which must not collapse a body to a line.
            foreach(float yaw in new[]{-39f,51f,141f,231f})
            {
                actor.transform.rotation=Quaternion.Euler(57,yaw,0);actor.sprite=solid;Project();var productionView=Render();
                Assert.That(Area(productionView),Is.GreaterThan(Area(baseline)*.65f),"The body silhouette collapsed at production camera yaw "+yaw);
                actor.sprite=hollow;Project();var productionHollow=Render();var hole=WorldAtUv(new Vector2(32f/128,35f/64));
                Assert.That(SampleAlpha(productionView,hole),Is.GreaterThan(4),"Production yaw hole probe missed its opaque negative control: "+yaw);
                Assert.That(SampleAlpha(productionHollow,hole),Is.LessThan(2),"Maintaining volume filled authored alpha holes at yaw "+yaw);
                actor.flipX=true;Project();Assert.That(Difference(productionHollow,Render()),Is.GreaterThan(.06f),"Maintaining volume lost sprite flip at yaw "+yaw);
                actor.flipX=false;
            }
            actor.sprite=solid;
            actor.transform.rotation=Quaternion.Euler(57,0,0);actor.transform.position=new Vector3(0,.6f,0);Project(.6f);var lifted=Render();
            AssertVisible(lifted,"lifted");Assert.That(lifted.Max(c=>c.a),Is.LessThan(baseline.Max(c=>c.a)),"A lifted body shadow should soften/fade.");
            Assert.That(Vector2.Distance(Centroid(baseline),Centroid(lifted)),Is.GreaterThan(2),"Lift must separate the projected body from the foot anchor.");
            var filter=shadow.GetComponent<MeshFilter>();
            Assert.That(filter.sharedMesh.vertices.All(v=>Mathf.Abs(filter.transform.TransformPoint(v).y)<.025f),Is.True,"Lifting the body lifted its ground shadow.");
            yield return null;
        }

        [UnityTest] public IEnumerator ProjectionClipsAtWaterUnequalHeightsAndTheMapEdge()
        {
            actor.sprite=solid;actor.transform.localScale=Vector3.one*1.5f;Project();var unclipped=Render();
            int outside=CountOutsideCenter(unclipped);Assert.That(outside,Is.GreaterThan(100),"The negative control must actually extend beyond the standing tile.");
            foreach(string boundary in new[]{"water","height","map edge"})
            {
                var map=FlatMap();
                foreach(var p in map.Tiles.Keys.Where(p=>p!=Vector2Int.zero).ToArray())
                    if(boundary=="map edge")map.Tiles.Remove(p);
                    else if(boundary=="water")map[p].Terrain=TerrainType.Water;
                    else map[p].Height=2;
                CreateShadow(map);var clipped=Render();AssertVisible(clipped,boundary);
                Assert.That(CountOutsideCenter(clipped),Is.LessThanOrEqualTo(4),"Body shadow bled over "+boundary+" instead of ending at the receiving land.");
                Assert.That(Area(clipped),Is.LessThan(Area(unclipped)*.85f),"The receiver boundary did not clip any projected pixels: "+boundary);
                actor.transform.position=Vector3.right;Project();Assert.That(shadow.GetComponent<MeshRenderer>().enabled,Is.False,"There is no dry receiver below the foot: "+boundary);
                actor.transform.position=Vector3.zero;
            }
            yield return null;
        }

        Color32[] Render()
        {
            var renderer=shadow.GetComponent<MeshRenderer>();Assert.That(renderer.enabled,Is.True);
            Assert.That(renderer.sharedMaterial.shader.isSupported,Is.True);
            var target=RenderTexture.GetTemporary(Size,Size,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var previous=RenderTexture.active;Texture2D readback=null;var commands=new CommandBuffer();
            try
            {
                commands.SetRenderTarget(target);commands.ClearRenderTarget(false,true,Color.clear);
                commands.SetViewProjectionMatrices(View,Projection);commands.DrawRenderer(renderer,renderer.sharedMaterial,0,0);
                Graphics.ExecuteCommandBuffer(commands);RenderTexture.active=target;
                readback=new Texture2D(Size,Size,TextureFormat.RGBA32,false,true);readback.ReadPixels(new Rect(0,0,Size,Size),0,0);readback.Apply();return readback.GetPixels32();
            }
            finally{commands.Release();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(readback!=null)Object.DestroyImmediate(readback);}
        }
        // Standard transparent blending stores squared alpha over a clear RT; lifted shadows
        // intentionally reach only a few alpha bytes even though they remain visible on terrain.
        static int Area(Color32[] pixels)=>pixels.Count(p=>p.a>1);
        static void AssertVisible(Color32[] pixels,string label)=>Assert.That(Area(pixels),Is.GreaterThan(40),"No visible production shadow rendered: "+label);
        static float Difference(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Math.Abs(x.a-y.a)).Sum()/(float)Math.Max(1,a.Sum(c=>(int)c.a));
        static Vector2 Centroid(Color32[] pixels)
        {
            double x=0,y=0,total=0;for(int i=0;i<pixels.Length;i++){double weight=pixels[i].a;x+=(i%Size)*weight;y+=(i/Size)*weight;total+=weight;}
            return new Vector2((float)(x/total),(float)(y/total));
        }
        int CountOutsideCenter(Color32[] pixels)
        {
            var inverse=(Projection*View).inverse;int outside=0;
            for(int i=0;i<pixels.Length;i++)if(pixels[i].a>2)
            {
                var p=inverse.MultiplyPoint(new Vector3(((i%Size)+.5f)/Size*2-1,((i/Size)+.5f)/Size*2-1,0));
                if(Mathf.Abs(p.x)>.52f||Mathf.Abs(p.z)>.52f)outside++;
            }
            return outside;
        }
        float SampleAlpha(Color32[] pixels,Vector3 point)
        {
            var clip=(Projection*View).MultiplyPoint(point);int x=Mathf.RoundToInt((clip.x*.5f+.5f)*Size-.5f),y=Mathf.RoundToInt((clip.y*.5f+.5f)*Size-.5f);float value=0;
            Assert.That(x,Is.InRange(2,Size-3));Assert.That(y,Is.InRange(2,Size-3));
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)value+=pixels[(y+dy)*Size+x+dx].a;
            return value/9;
        }
        Vector3 WorldAtUv(Vector2 point)
        {
            var filter=shadow.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;var uv=mesh.uv;var indices=mesh.triangles;var vertices=mesh.vertices;
            for(int i=0;i<indices.Length;i+=3)
            {
                int ia=indices[i],ib=indices[i+1],ic=indices[i+2];var ab=uv[ib]-uv[ia];var ac=uv[ic]-uv[ia];var ap=point-uv[ia];
                float d=ab.x*ac.y-ab.y*ac.x;if(Mathf.Abs(d)<.000001f)continue;
                float b=(ap.x*ac.y-ap.y*ac.x)/d,c=(ab.x*ap.y-ab.y*ap.x)/d;
                if(b>=-.0001f&&c>=-.0001f&&b+c<=1.0001f)return filter.transform.TransformPoint(vertices[ia]+b*(vertices[ib]-vertices[ia])+c*(vertices[ic]-vertices[ia]));
            }
            Assert.Fail("The alpha-hole probe was clipped out of the fixture.");return Vector3.zero;
        }
    }

    public partial class BattleSceneTests
    {
        string DepthUnitState()=>string.Join("\n",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))));
        string DepthGridState()=>string.Join(";",director.Session.Grid.Tiles.Values.OrderBy(t=>t.Coordinate.x).ThenBy(t=>t.Coordinate.y)
            .Select(t=>t.Coordinate+":"+t.Height+":"+t.MovementCost+":"+t.Walkable+":"+t.Terrain+":"+(t.Occupant==null?-1:director.Session.Units.IndexOf(t.Occupant))));
        [UnityTest] public IEnumerator CharacterDepthCameraAndPoseUpdatesPreserveSelectionCombatGridAndRandomState()
        {
            director.BeginBattle();yield return new WaitForEndOfFrame();director.StopAllCoroutines();
            var active=director.Session.Active;var state=director.State;
            string units=DepthUnitState(),grid=DepthGridState();
            string campaign=JsonUtility.ToJson(director.Campaign),random=JsonUtility.ToJson(UnityEngine.Random.state);int draws=director.Session.Resolver.RandomDraws,picks=0;
            var motion=director.Board.GetComponentsInChildren<CharacterMotion>().Single(m=>m.Owner==active);
            var shadows=director.Board.GetComponentsInChildren<ProjectedUnitShadow>();
            Assert.That(shadows.Length,Is.GreaterThanOrEqualTo(director.Session.Units.Count(u=>u.Alive&&!u.CrossoverReserve)));
            foreach(var projection in shadows)Assert.That(projection.GetComponentsInChildren<Collider>(),Is.Empty,"A visual shadow must not intercept selection rays.");
            for(int quadrant=0;quadrant<4;quadrant++)
            {
                director.Board.RotateCamera(90);director.Board.SetAnimation(active,quadrant%2==0?AnimationKind.Damage:AnimationKind.Idle);
                if(quadrant%2==0)motion.Impact(Vector3.back,false);
                yield return new WaitForEndOfFrame();
                foreach(var tile in director.Session.Grid.Tiles.Values.Where(t=>t.Walkable))
                {
                    var screen=director.Board.BattleCamera.WorldToScreenPoint(tile.WorldPosition(director.Catalog.Rules.TileHeight));
                    if(screen.z<=0||!director.Board.BattleCamera.pixelRect.Contains(screen))continue;
                    Assert.That(director.Board.Pick(screen,out var hit)&&hit==tile.Coordinate,Is.True,"Visual depth changed tile picking at "+tile.Coordinate);picks++;
                }
                Assert.That(director.Session.Active,Is.SameAs(active));Assert.That(director.State,Is.SameAs(state));
                Assert.That(DepthUnitState(),Is.EqualTo(units));Assert.That(DepthGridState(),Is.EqualTo(grid));
                Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(draws));Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(random));
                Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));
            }
            Assert.That(picks,Is.GreaterThan(100));
        }

        [UnityTest] public IEnumerator CharacterDepthHidesWithItsActorAndReleasesOwnedRenderingResources()
        {
            director.BeginBattle();yield return new WaitForEndOfFrame();director.StopAllCoroutines();
            var unit=director.Session.Active;var motion=director.Board.GetComponentsInChildren<CharacterMotion>().Single(m=>m.Owner==unit);var sprite=motion.GetComponent<SpriteRenderer>();
            var grounding=motion.GetComponentInParent<GroundedUnitVisual>();var projection=grounding.GetComponentInChildren<ProjectedUnitShadow>(true);
            Assert.That(projection,Is.Not.Null);var renderer=projection.GetComponent<MeshRenderer>();Assert.That(renderer.enabled,Is.True);
            sprite.enabled=false;yield return new WaitForEndOfFrame();Assert.That(renderer.enabled,Is.False,"A hidden actor retained its body shadow.");
            sprite.enabled=true;yield return new WaitForEndOfFrame();Assert.That(renderer.enabled,Is.True);
            int hp=unit.CurrentHP;unit.CurrentHP=0;yield return new WaitForEndOfFrame();Assert.That(renderer.enabled&&renderer.gameObject.activeInHierarchy,Is.False,"A defeated actor retained its body shadow.");
            unit.CurrentHP=hp;yield return new WaitForEndOfFrame();Assert.That(renderer.enabled,Is.True);
            var components=director.Board.GetComponentsInChildren<ProjectedUnitShadow>(true);
            var meshes=components.Select(p=>p.GetComponent<MeshFilter>().sharedMesh).ToArray();var materials=components.Select(p=>p.GetComponent<MeshRenderer>().sharedMaterial).Distinct().ToArray();
            Assert.That(meshes.All(m=>m!=null),Is.True);Assert.That(materials.All(m=>m!=null),Is.True);
            director.Restart();yield return null;yield return null;
            Assert.That(components.All(p=>p==null),Is.True);Assert.That(meshes.All(m=>m==null),Is.True,"Restart leaked a projected shadow mesh.");
            Assert.That(materials.All(m=>m==null),Is.True,"Restart leaked a projected shadow material.");
        }
    }
}
