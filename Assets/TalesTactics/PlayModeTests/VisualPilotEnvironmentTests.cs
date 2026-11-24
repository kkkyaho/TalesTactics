using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public sealed class VisualPilotEnvironmentTests
    {
        readonly List<PixelBattlefield> scenery=new List<PixelBattlefield>();
        Camera camera;
        Material fallback;

        [UnitySetUp] public IEnumerator SetUp()
        {
            camera=new GameObject("Visual pilot terrain camera").AddComponent<Camera>();
            camera.enabled=false;camera.transform.rotation=Quaternion.Euler(57,-39,0);
            var shader=Resources.Load<Shader>("TalesTactics/TerrainArt/Terrain");
            Assert.That(shader,Is.Not.Null);fallback=new Material(shader);
            yield return null;
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            foreach(var item in scenery)if(item!=null)Object.Destroy(item.gameObject);
            scenery.Clear();
            if(camera!=null)Object.Destroy(camera.gameObject);
            if(fallback!=null)Object.Destroy(fallback);
            yield return null;yield return null;
        }

        [UnityTest] public IEnumerator OnlyEpisodeSixReceivesTheQuieterFloorWithoutLeakingToSharedThemes()
        {
            var first=Create(CrossoverCampaign.FirstStage);
            var pilot=Create(CrossoverCampaign.FirstStage+5);
            var tile=new GridTile{Coordinate=Vector2Int.zero};
            var original=first.Surface(tile);var changed=pilot.Surface(tile);
            Assert.That(first.IsVisualPilotStage,Is.False);
            Assert.That(pilot.IsVisualPilotStage,Is.True);
            Assert.That(first.Chapter,Is.EqualTo(pilot.Chapter),"The same ruins theme must not opt every stage into the pilot.");
            foreach(string property in new[]{"_DetailContrast","_SurfaceRelief","_Dapple","_ArtSaturation"})
                Assert.That(changed.GetFloat(property),Is.LessThan(original.GetFloat(property)),property);
            Assert.That(changed.mainTexture,Is.SameAs(original.mainTexture),"The shared floor artwork must remain intact.");
            foreach(string property in new[]{"_KeyColor","_FillColor","_KeyDirection"})
                Assert.That(changed.GetVector(property),Is.EqualTo(original.GetVector(property)),property);

            // Build these after the pilot to detect changes to shared materials or palette state.
            var controls=new List<int>{CrossoverCampaign.FirstStage,CrossoverCampaign.FirstStage+4,CrossoverCampaign.FirstStage+6};
            controls.AddRange(Enumerable.Range(PersonalScenarios.FirstStage,PersonalScenarios.Count));
            foreach(int chapter in controls)
            {
                var other=Create(chapter);var floor=other.Surface(tile);
                bool verdant=BattleEnvironmentPalette.Verdant(BattleEnvironmentPalette.Theme(chapter));
                Assert.That(other.IsVisualPilotStage,Is.False,"Unexpected pilot stage "+chapter);
                Assert.That(floor.GetFloat("_DetailContrast"),Is.EqualTo(.76f).Within(.0001f),"Detail contrast at "+chapter);
                Assert.That(floor.GetFloat("_SurfaceRelief"),Is.EqualTo(.22f).Within(.0001f),"Relief at "+chapter);
                Assert.That(floor.GetFloat("_Dapple"),Is.EqualTo(verdant?.11f:.025f).Within(.0001f),"Dapple at "+chapter);
                Assert.That(floor.GetFloat("_ArtSaturation"),Is.EqualTo(verdant?.78f:.88f).Within(.0001f),"Saturation at "+chapter);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator PilotReducesSmallFoliageWhileKeepingWallsGridAndRandomState()
        {
            int chapter=CrossoverCampaign.FirstStage+5;
            var map=CrossoverCampaign.Map(chapter);
            string before=MapDigest(map);var randomBefore=Random.state;
            // Render the same map under the original ruins presentation and the opted-in chapter.
            var original=Create(CrossoverCampaign.FirstStage);var pilot=Create(chapter);
            foreach(var item in new[]{original,pilot})
            {
                foreach(var tile in map.Tiles.Values)item.Decorate(tile,map,.5f);
                item.Complete(map,.5f);
                Assert.That(item.GroundMap,Is.SameAs(map));
                Assert.That(item.GetComponentsInChildren<Collider>(),Is.Empty,"Decorations must not add picking or movement colliders.");
            }
            int originalFoliage=Details(original,"Foliage detail "),pilotFoliage=Details(pilot,"Foliage detail ");
            Assert.That(originalFoliage,Is.GreaterThan(0),"The control must actually contain foliage.");
            Assert.That(pilotFoliage,Is.GreaterThan(0).And.LessThan(originalFoliage),"Reduce small clutter without removing the environment.");
            Assert.That(Details(pilot,"Architectural detail "),Is.EqualTo(Details(original,"Architectural detail ")),"Keep the authored obstacle silhouettes.");
            Assert.That(pilot.CanopyCount,Is.EqualTo(original.CanopyCount),"Keep the exterior tree silhouette.");
            Assert.That(MapDigest(map),Is.EqualTo(before),"Visual changes must preserve tile rules and occupants.");
            Assert.That(Random.state,Is.EqualTo(randomBefore),"Decoration must not consume gameplay randomness.");
            yield return null;
        }

        PixelBattlefield Create(int chapter)
        {
            var item=new GameObject("Visual pilot terrain "+chapter).AddComponent<PixelBattlefield>();scenery.Add(item);
            item.Initialize(chapter,fallback,fallback,camera);return item;
        }
        static int Details(PixelBattlefield item,string prefix)=>item.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith(prefix));
        static string MapDigest(GridMap map)=>string.Join(";",map.Tiles.Values.OrderBy(t=>t.Coordinate.x).ThenBy(t=>t.Coordinate.y)
            .Select(t=>t.Coordinate+":"+t.Height+":"+t.Walkable+":"+t.MovementCost+":"+t.Terrain+":"+(t.Occupant==null?"empty":t.Occupant.Data.Id)));
    }
}
