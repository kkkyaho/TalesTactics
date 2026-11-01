using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public sealed class TerrainPresentationTests
    {
        Camera camera;
        Material fallback;
        PixelBattlefield first,second;

        [UnitySetUp] public IEnumerator SetUpTerrain()
        {
            camera=new GameObject("Terrain presentation test camera").AddComponent<Camera>();
            camera.enabled=false;camera.transform.rotation=Quaternion.Euler(57,45,0);
            var shader=Resources.Load<Shader>("TalesTactics/TerrainArt/Terrain");
            Assert.That(shader,Is.Not.Null);fallback=new Material(shader);
            yield return null;
        }
        [UnityTearDown] public IEnumerator TearDownTerrain()
        {
            DestroyScenery();
            if(camera!=null)Object.Destroy(camera.gameObject);
            if(fallback!=null)Object.Destroy(fallback);
            yield return null;yield return null;
        }

        [UnityTest] public IEnumerator EveryCampaignTerrainRebuildIsDeterministicAndCannotChangeRulesOrPicking()
        {
            for(int stage=0;stage<CampaignStages.Count;stage++)
            {
                var map=CampaignContent.Map(stage);
                var original=map.Tiles.Values.Select(t=>new {t.Coordinate,t.Height,t.MovementCost,t.Walkable,t.Terrain,t.Occupant}).ToArray();
                string random=JsonUtility.ToJson(Random.state);
                try
                {
                    first=Build(stage,map);second=Build(stage,map);
                    if(first.Chapter==0||first.Chapter==1)
                        Assert.That(first.Surface(map.Tiles.Values.First(t=>t.Terrain!=TerrainType.Water)).mainTexture.name,Is.EqualTo("RuinsFloorWeathered"),"Natural ruins must use the non-repeating authored floor");
                    Assert.That(Digest(first),Is.EqualTo(Digest(second)),"Rebuild changed scenery at stage "+stage);
                    Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random),"Presentation consumed gameplay RNG at stage "+stage);
                    Assert.That(map.Tiles.Count,Is.EqualTo(original.Length));
                    foreach(var before in original)
                    {
                        var after=map[before.Coordinate];Assert.That(after,Is.Not.Null);
                        Assert.That(after.Height,Is.EqualTo(before.Height));Assert.That(after.MovementCost,Is.EqualTo(before.MovementCost));
                        Assert.That(after.Walkable,Is.EqualTo(before.Walkable));Assert.That(after.Terrain,Is.EqualTo(before.Terrain));
                        Assert.That(after.Occupant,Is.SameAs(before.Occupant));
                    }
                    Assert.That(first.GetComponentsInChildren<Collider>(),Is.Empty,"Decoration must not intercept tile picks");
                    Assert.That(first.DecorationMeshCount,Is.InRange(2,17),"Relief should reuse the existing material batches");
                    AssertBoundedTerrainGeometry(first,map,stage);
                    var tall=first.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("Forest canopy ")&&r.name!="Forest canopy 3").ToArray();
                    int maxX=map.Tiles.Keys.Max(p=>p.x),maxZ=map.Tiles.Keys.Max(p=>p.y);
                    foreach(var tree in tall)
                    {
                        var p=tree.transform.localPosition;
                        Assert.That(p.x<-.5f||p.x>maxX+.5f||p.z<-.5f||p.z>maxZ+.5f,Is.True,"Tall scenery entered the board");
                    }
                    // A minimum crown spacing catches a return to overlapping repeated tree rows.
                    for(int i=0;i<tall.Length;i++)for(int j=i+1;j<tall.Length;j++)
                    {
                        var a=tall[i].transform.localPosition;var b=tall[j].transform.localPosition;
                        float ha=tall[i].sprite.bounds.size.y*tall[i].transform.localScale.y;
                        float hb=tall[j].sprite.bounds.size.y*tall[j].transform.localScale.y;
                        Assert.That(Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z)),Is.GreaterThanOrEqualTo((ha+hb)*.40f-.001f));
                    }
                    if(stage==0)Assert.That(first.CanopyCount,Is.GreaterThan(10),"The forest should retain a populated silhouette");
                }
                finally{DestroyScenery();}
                yield return null;yield return null;
            }
        }

        [UnityTest] public IEnumerator HeightReliefUsesTheActualDropAndKeepsLargeDropsAndStandingCentersClear()
        {
            var map=new GridMap();
            for(int x=0;x<5;x++)for(int z=0;z<5;z++)
            {
                var p=new Vector2Int(x,z);map.Tiles.Add(p,new GridTile{Coordinate=p});
            }
            var upper=map[new Vector2Int(2,2)];upper.Height=2;
            first=Create(0);first.Decorate(upper,map,.5f);first.Complete(map,.5f);
            var vertices=first.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Architecture batch ")).SelectMany(f=>f.sharedMesh.vertices).ToArray();
            foreach(var direction in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
            {
                var tangent=Vector3.Cross(direction,Vector3.up);
                var edge=upper.WorldPosition(.5f)+direction*.503f;
                foreach(float side in new[]{-1f,1f})foreach(float height in new[]{0f,1f})
                {
                    var expected=edge+tangent*(side*.499f);expected.y=height;
                    Assert.That(vertices.Any(v=>(v-expected).sqrMagnitude<.0000001f),Is.True,"Missing full-height riser corner "+expected);
                }
                // No decorative tread can suggest a walkable staircase across a two-level drop.
                Assert.That(vertices.Any(v=>Vector3.Dot(v-new Vector3(2,0,2),direction)>.54f&&
                    Vector3.Dot(v-new Vector3(2,0,2),direction)<.9f&&Mathf.Abs(Vector3.Dot(v-new Vector3(2,0,2),tangent))<.49f&&v.y>.001f),Is.False);
            }
            Assert.That(vertices.Any(v=>Mathf.Abs(v.x-2)<.3f&&Mathf.Abs(v.z-2)<.3f&&v.y>1f),Is.False,"Decoration covered the unit standing center");
            Assert.That(first.GetComponentsInChildren<Collider>(),Is.Empty);
            yield return null;
        }

        [UnityTest] public IEnumerator FracturedPerimetersStayBelowStandingSpaceAndHaveLowIrregularSilhouettes()
        {
            var map=new GridMap();
            for(int x=0;x<5;x++)for(int z=0;z<5;z++)
            {
                var p=new Vector2Int(x,z);map.Tiles.Add(p,new GridTile{Coordinate=p});
            }
            var standingOffsets=new[]{Vector2.zero,new Vector2(-.28f,-.28f),new Vector2(.28f,-.28f),new Vector2(-.28f,.28f),new Vector2(.28f,.28f)};
            for(int theme=0;theme<6;theme++)
            {
                try
                {
                    first=Build(theme,map);var filters=TerrainBatches(first);
                    foreach(var filter in filters)
                    {
                        var mesh=filter.sharedMesh;var vertices=mesh.vertices;var indices=mesh.triangles;
                        for(int triangle=0;triangle<indices.Length;triangle+=3)
                        {
                            var a=vertices[indices[triangle]];var b=vertices[indices[triangle+1]];var c=vertices[indices[triangle+2]];
                            if(Mathf.Max(a.y,Mathf.Max(b.y,c.y))<-.01f)continue;
                            foreach(var tile in map.Tiles.Values)
                            foreach(var offset in standingOffsets)
                            {
                                var point=new Vector2(tile.Coordinate.x,tile.Coordinate.y)+offset;
                                Assert.That(CoversStandingPoint(a,b,c,point),Is.False,
                                    "Theme "+theme+" decoration intrudes into standing space at "+point+" in "+filter.name);
                            }
                        }
                    }
                    var points=filters.SelectMany(f=>f.sharedMesh.vertices).ToArray();
                    var center=new Vector3(2,0,2);
                    foreach(var direction in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
                    {
                        var across=Vector3.Cross(direction,Vector3.up);
                        var silhouette=points.Where(v=>Vector3.Dot(v-center,direction)>2.51f&&
                            Mathf.Abs(Vector3.Dot(v-center,across))<1.6f&&v.y<-.3f).ToArray();
                        Assert.That(silhouette.Length,Is.GreaterThan(8),"Missing low outside terrain in theme "+theme);
                        Assert.That(silhouette.Max(v=>Vector3.Dot(v-center,direction))-silhouette.Min(v=>Vector3.Dot(v-center,direction)),
                            Is.GreaterThan(.20f),"Outside terrain regressed to a straight vertical curtain");
                        Assert.That(silhouette.Max(v=>v.y)-silhouette.Min(v=>v.y),Is.GreaterThan(.5f),"Outside terrain lost its connected lower silhouette");
                    }
                    Assert.That(first.GetComponentsInChildren<Collider>(),Is.Empty);
                }
                finally{DestroyScenery();}
                yield return null;yield return null;
            }
        }

        [UnityTest] public IEnumerator UnevenDryAndWaterPerimetersJoinEveryExposedProfileEdge()
        {
            foreach(int theme in new[]{0,3})
            foreach(var outward in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
            for(int boundary=0;boundary<3;boundary++)
            {
                var map=new GridMap();
                for(int x=0;x<5;x++)for(int z=0;z<5;z++)
                {
                    var coordinate=new Vector2Int(x,z);map.Tiles.Add(coordinate,new GridTile{Coordinate=coordinate});
                }
                var rim=outward.x==0?new Vector2Int(2,outward.y<0?0:4):new Vector2Int(outward.x<0?0:4,2);
                var along=new Vector2Int(-outward.y,outward.x);var neighbor=map[rim+along];
                map[rim].Height=boundary==2?0:2;neighbor.Height=boundary==2?2:0;
                if(boundary>0)neighbor.Terrain=TerrainType.Water;
                try
                {
                    first=Build(theme,map);
                    var origin=new Vector3(rim.x,0,rim.y);var direction=new Vector3(outward.x,0,outward.y);
                    var tangent=new Vector3(along.x,0,along.y);var joint=origin+tangent*.5f;
                    var allEdges=new List<BoundaryEdge>();var profiles=new List<BoundaryEdge>();
                    // Authored facade decals are deliberately open, so inspect structural material
                    // batches only. Waterfall ends sit .02 inside their tile and share this band.
                    foreach(var filter in first.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Architecture batch ")))
                    {
                        var mesh=filter.sharedMesh;var vertices=mesh.vertices;var indices=mesh.triangles;
                        for(int triangle=0;triangle<indices.Length;triangle+=3)
                        {
                            var a=vertices[indices[triangle]];var b=vertices[indices[triangle+1]];var c=vertices[indices[triangle+2]];
                            if(Vector3.Cross(b-a,c-a).sqrMagnitude<.00000001f)continue;
                            bool extendsPastJoint=Mathf.Max(Mathf.Abs(Vector3.Dot(a-joint,tangent)),
                                Mathf.Max(Mathf.Abs(Vector3.Dot(b-joint,tangent)),Mathf.Abs(Vector3.Dot(c-joint,tangent))))>.10f;
                            foreach(var edge in new[]{new BoundaryEdge(a,b),new BoundaryEdge(b,c),new BoundaryEdge(c,a)})
                            {
                                if(Mathf.Abs(Vector3.Dot(edge.A-joint,tangent))>.021f||Mathf.Abs(Vector3.Dot(edge.B-joint,tangent))>.021f)continue;
                                allEdges.Add(edge);
                                float from=Vector3.Dot(edge.A-origin,direction),to=Vector3.Dot(edge.B-origin,direction);
                                if(extendsPastJoint&&Mathf.Min(from,to)>=.502f&&Mathf.Max(from,to)<=1.1f&&(edge.B-edge.A).sqrMagnitude>.0025f)
                                    profiles.Add(edge);
                            }
                        }
                    }
                    string label="theme "+theme+", outward "+outward+", dry/wet case "+boundary;
                    Assert.That(profiles.Count,Is.GreaterThanOrEqualTo(4),"Missing actual exterior face profiles: "+label);
                    // Negative control removes the joining faces but retains the actual side faces;
                    // it must expose a gap, otherwise this fixture would not exercise the joint.
                    Assert.That(profiles.Any(edge=>UnpairedEdgeLength(edge,profiles)>.01f),Is.True,"Joint fixture did not exercise an open profile: "+label);
                    foreach(var edge in profiles)
                        Assert.That(UnpairedEdgeLength(edge,allEdges),Is.LessThan(.0002f),"Open exterior profile "+edge.A+" -> "+edge.B+"; "+label);
                }
                finally{DestroyScenery();}
                yield return null;yield return null;
            }
        }

        [UnityTest] public IEnumerator WorldSpaceFloorWaterAndWaterfallsDoNotRestartAtTileUvSeams()
        {
            Assert.That(fallback.shader.isSupported,Is.True);
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var material=new Material(fallback);var whole=SurfaceProbeMesh(1);var tiled=SurfaceProbeMesh(4);
            try
            {
                var texels=new Color[64*64];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                    texels[y*64+x]=new Color(.15f+x/63f*.72f,.12f+y/63f*.74f,.25f+(x+y)/126f*.48f,1);
                texture.SetPixels(texels);texture.Apply();material.mainTexture=texture;
                material.SetTexture("_MeadowTex",texture);material.SetFloat("_Unlit",1);
                material.SetVector("_AtlasRect",new Vector4(0,0,1,1));material.SetVector("_FloorExtent",new Vector4(8,8,0,0));
                var horizontal=Matrix4x4.TRS(new Vector3(2,0,3),Quaternion.Euler(90,0,0),Vector3.one);
                // Negative control proves this fixture actually detects UVs restarting on each cell.
                var localWhole=RenderSurfaceProbe(whole,material,horizontal);var localTiled=RenderSurfaceProbe(tiled,material,horizontal);
                AssertSurfaceRendered(localWhole,"cell-local whole");AssertSurfaceRendered(localTiled,"cell-local tiled");
                float localDifference=MeanPixelDifference(localWhole,localTiled);TestContext.WriteLine("Local UV negative control mean RGB difference: "+localDifference);
                Assert.That(localDifference,Is.GreaterThan(8),"The seam probe must distinguish cell-local texture coordinates");
                for(int surface=0;surface<4;surface++)
                {
                    material.SetFloat("_WorldFloor",surface==0?1:0);material.SetFloat("_LivingFloor",surface==0?1:0);
                    material.SetFloat("_Water",surface==0?0:1);
                    var model=surface<2?horizontal:Matrix4x4.TRS(new Vector3(2,-2,3),surface==2?Quaternion.identity:Quaternion.Euler(0,90,0),Vector3.one);
                    // Both synchronous draws use the same engine time and identical world positions,
                    // normals and colors. Only the number of cells and their reset UVs are different.
                    var continuous=RenderSurfaceProbe(whole,material,model);var split=RenderSurfaceProbe(tiled,material,model);
                    AssertSurfaceRendered(continuous,"continuous "+surface);AssertSurfaceRendered(split,"split "+surface);
                    float difference=MeanPixelDifference(continuous,split);TestContext.WriteLine("World-space surface "+surface+" mean RGB difference: "+difference);
                    Assert.That(difference,Is.LessThan(.65f),"Floor/water appearance restarted at tile boundaries, surface "+surface);
                }
            }
            finally{Object.DestroyImmediate(whole);Object.DestroyImmediate(tiled);Object.DestroyImmediate(material);Object.DestroyImmediate(texture);}
            yield return null;
        }

        readonly struct BoundaryEdge
        {
            public readonly Vector3 A,B;
            public BoundaryEdge(Vector3 a,Vector3 b){A=a;B=b;}
        }
        static float UnpairedEdgeLength(BoundaryEdge edge,IEnumerable<BoundaryEdge> candidates)
        {
            float length=Vector3.Distance(edge.A,edge.B);var direction=(edge.B-edge.A)/length;
            var intervals=new List<Vector2>();var cuts=new List<float>{0,1};
            foreach(var other in candidates)
            {
                // Permit a long waterfall edge to meet several shorter connector edges. Exact
                // vertex-count matching would falsely reject this watertight geometric T-joint.
                if(Vector3.Cross(other.A-edge.A,direction).sqrMagnitude>.00000004f||
                    Vector3.Cross(other.B-edge.A,direction).sqrMagnitude>.00000004f)continue;
                float a=Vector3.Dot(other.A-edge.A,direction)/length,b=Vector3.Dot(other.B-edge.A,direction)/length;
                float low=Mathf.Clamp01(Mathf.Min(a,b)),high=Mathf.Clamp01(Mathf.Max(a,b));
                if(high-low<.000001f)continue;intervals.Add(new Vector2(low,high));cuts.Add(low);cuts.Add(high);
            }
            cuts.Sort();float unpaired=0;
            for(int i=1;i<cuts.Count;i++)
            {
                if(cuts[i]-cuts[i-1]<.000001f)continue;float middle=(cuts[i]+cuts[i-1])*.5f;
                if(intervals.Count(interval=>interval.x<=middle&&interval.y>=middle)<2)unpaired+=(cuts[i]-cuts[i-1])*length;
            }
            return unpaired;
        }

        static MeshFilter[] TerrainBatches(PixelBattlefield scenery)=>scenery.GetComponentsInChildren<MeshFilter>()
            .Where(f=>f.name.StartsWith("Architecture ")||f.name=="Continuous forest surroundings").ToArray();
        static void AssertBoundedTerrainGeometry(PixelBattlefield scenery,GridMap map,int stage)
        {
            var filters=TerrainBatches(scenery);int total=0;
            float minX=map.Tiles.Keys.Min(p=>p.x),maxX=map.Tiles.Keys.Max(p=>p.x);
            float minZ=map.Tiles.Keys.Min(p=>p.y),maxZ=map.Tiles.Keys.Max(p=>p.y);
            float low=map.Tiles.Values.Min(t=>t.Height)*.5f-7,high=map.Tiles.Values.Max(t=>t.Height)*.5f+1;
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;total+=mesh.vertexCount;
                Assert.That(mesh.vertexCount,Is.LessThanOrEqualTo(65535),"A terrain batch must retain a 16-bit index budget at stage "+stage);
                Assert.That(mesh.indexFormat,Is.EqualTo(UnityEngine.Rendering.IndexFormat.UInt16));
                // Digest above already checks every vertex for finiteness; use the enclosing bounds
                // here so all 56 campaign maps do not repeat millions of assertion allocations.
                var bounds=mesh.bounds;AssertFinite(bounds.min);AssertFinite(bounds.max);
                Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(minX-6));Assert.That(bounds.max.x,Is.LessThanOrEqualTo(maxX+6));
                Assert.That(bounds.min.z,Is.GreaterThanOrEqualTo(minZ-6));Assert.That(bounds.max.z,Is.LessThanOrEqualTo(maxZ+6));
                Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(low));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(high));
            }
            Assert.That(total,Is.LessThan(60000),"Decorative relief exceeded the small-board geometry budget at stage "+stage);
            TestContext.WriteLine("Terrain geometry stage "+stage+": "+total+" vertices in "+filters.Length+" batches");
        }
        static bool CoversStandingPoint(Vector3 a,Vector3 b,Vector3 c,Vector2 point)
        {
            var ab=new Vector2(b.x-a.x,b.z-a.z);var ac=new Vector2(c.x-a.x,c.z-a.z);var delta=point-new Vector2(a.x,a.z);
            float determinant=ab.x*ac.y-ab.y*ac.x;if(Mathf.Abs(determinant)<.000001f)return false;
            float u=(delta.x*ac.y-delta.y*ac.x)/determinant,v=(ab.x*delta.y-ab.y*delta.x)/determinant;
            return u>=0&&v>=0&&u+v<=1&&a.y+u*(b.y-a.y)+v*(c.y-a.y)>=-.01f;
        }
        static Mesh SurfaceProbeMesh(int divisions)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var normals=new List<Vector3>();var indices=new List<int>();
            for(int y=0;y<divisions;y++)for(int x=0;x<divisions;x++)
            {
                int start=vertices.Count;float left=x*4f/divisions,right=(x+1)*4f/divisions,bottom=y*4f/divisions,top=(y+1)*4f/divisions;
                vertices.AddRange(new[]{new Vector3(left,bottom,0),new Vector3(left,top,0),new Vector3(right,top,0),new Vector3(right,bottom,0)});
                uv.AddRange(new[]{new Vector2(.01f,.01f),new Vector2(.01f,.99f),new Vector2(.99f,.99f),new Vector2(.99f,.01f)});
                for(int i=0;i<4;i++){colors.Add(Color.white);normals.Add(Vector3.back);}
                indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            var mesh=new Mesh{name="Terrain coordinate continuity probe"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);
            mesh.SetNormals(normals);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();return mesh;
        }
        static Color32[] RenderSurfaceProbe(Mesh mesh,Material material,Matrix4x4 model)
        {
            var target=RenderTexture.GetTemporary(128,128,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var previous=RenderTexture.active;Texture2D readback=null;var commands=new UnityEngine.Rendering.CommandBuffer();
            try
            {
                commands.SetRenderTarget(target);commands.ClearRenderTarget(true,true,Color.clear);
                var projection=Matrix4x4.Ortho(0,4,0,4,-10,10);var gpuProjection=GL.GetGPUProjectionMatrix(projection,true);
                // RenderTexture projection flips Y on D3D. Unlike the two-sided VFX probe, terrain
                // culls backfaces; its custom view must explicitly compensate for that reflection.
                commands.SetInvertCulling(gpuProjection.m11*projection.m11<0);
                commands.SetViewProjectionMatrices(model.inverse,gpuProjection);
                commands.DrawMesh(mesh,model,material,0,0);commands.SetInvertCulling(GL.invertCulling);
                Graphics.ExecuteCommandBuffer(commands);RenderTexture.active=target;
                readback=new Texture2D(128,128,TextureFormat.RGBA32,false,true);readback.ReadPixels(new Rect(0,0,128,128),0,0);readback.Apply();return readback.GetPixels32();
            }
            finally{commands.Release();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(readback!=null)Object.DestroyImmediate(readback);}
        }
        static void AssertSurfaceRendered(Color32[] pixels,string label)
        {
            int opaque=pixels.Count(p=>p.a>250),colors=pixels.Distinct().Count();
            Assert.That(opaque,Is.GreaterThan(15000),"Probe did not render a visible surface: "+label+"; opaque="+opaque+", colors="+colors);
            Assert.That(colors,Is.GreaterThan(32),"A blank or flat render cannot establish surface continuity: "+label);
        }
        static float MeanPixelDifference(Color32[] a,Color32[] b)
        {
            long difference=0;int samples=0;
            for(int y=2;y<126;y++)for(int x=2;x<126;x++)
            {
                int index=y*128+x;difference+=Mathf.Abs(a[index].r-b[index].r)+Mathf.Abs(a[index].g-b[index].g)+Mathf.Abs(a[index].b-b[index].b);samples+=3;
            }
            return (float)difference/samples;
        }

        PixelBattlefield Create(int stage)
        {
            var scenery=new GameObject("Isolated terrain presentation").AddComponent<PixelBattlefield>();
            try{scenery.Initialize(stage,fallback,fallback,camera);return scenery;}
            catch{Object.Destroy(scenery.gameObject);throw;}
        }
        PixelBattlefield Build(int stage,GridMap map)
        {
            var scenery=Create(stage);
            try
            {
                foreach(var tile in map.Tiles.Values)scenery.Decorate(tile,map,.5f);
                scenery.Complete(map,.5f);return scenery;
            }
            catch{Object.Destroy(scenery.gameObject);throw;}
        }
        void DestroyScenery()
        {
            if(first!=null)Object.Destroy(first.gameObject);
            if(second!=null)Object.Destroy(second.gameObject);
            first=null;second=null;
        }
        static string Digest(PixelBattlefield scenery)
        {
            using(var buffer=new MemoryStream())
            using(var writer=new BinaryWriter(buffer))
            {
                foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>())
                {
                    writer.Write(filter.name);var mesh=filter.sharedMesh;
                    foreach(var v in new[]{filter.transform.localPosition,filter.transform.localScale,filter.transform.localEulerAngles})
                    {AssertFinite(v);writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
                    writer.Write(mesh.vertexCount);
                    foreach(var v in mesh.vertices){AssertFinite(v);writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
                    foreach(var uv in mesh.uv){writer.Write(uv.x);writer.Write(uv.y);}
                    foreach(int index in mesh.triangles)writer.Write(index);
                }
                foreach(var renderer in scenery.GetComponentsInChildren<SpriteRenderer>())
                {
                    writer.Write(renderer.name);writer.Write(renderer.flipX);writer.Write(renderer.flipY);
                    foreach(var v in new[]{renderer.transform.localPosition,renderer.transform.localScale,renderer.transform.localEulerAngles})
                    {AssertFinite(v);writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
                    var rect=renderer.sprite.rect;writer.Write(rect.x);writer.Write(rect.y);writer.Write(rect.width);writer.Write(rect.height);
                }
                writer.Flush();using(var sha=SHA256.Create())return System.Convert.ToBase64String(sha.ComputeHash(buffer.ToArray()));
            }
        }
        static void AssertFinite(Vector3 v)
        {
            Assert.That(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.z),Is.False);
        }
    }
}
