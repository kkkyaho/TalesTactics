using System.Collections;
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
