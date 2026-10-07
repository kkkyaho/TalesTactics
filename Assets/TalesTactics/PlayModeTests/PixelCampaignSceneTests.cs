using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator PixelRangesStayReadableAndReleaseSceneryAcrossRestarts()
        {
            director.TrainingMode=false;director.Campaign=new CampaignSave();director.PersistCampaign=_=>false;
            for(int s=0;s<6;s++)director.Campaign.StoryProgress.Add(CampaignStages.Id(s));
            for(int cycle=0;cycle<12;cycle++)
            {
                director.SelectedStage=cycle%6;director.BeginBattle();yield return null;
                var active=director.Session.Active;var origin=active.Position;
                var hp=director.Session.Units.Select(u=>u.CurrentHP).ToArray();
                Click("Move / 이동");yield return null;
                var reachable=director.Session.Grid.Reachable(active,out _).Keys.ToArray();
                var borders=Object.FindObjectsByType<LineRenderer>().Where(r=>r.name.StartsWith("Range border ")).ToArray();
                var shown=borders.Where(r=>r.enabled).ToArray();
                CollectionAssert.AreEquivalent(reachable,shown.Select(r=>r.GetComponentInParent<TileView>().Coordinate).ToArray());
                foreach(var r in shown)
                {
                    Assert.That(r.GetComponents<Collider>(),Is.Empty);
                    Assert.That(r.sharedMaterial,Is.SameAs(director.Board.HighlightMaterial));
                    var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);
                    Assert.That(block.GetColor("_BaseColor").maxColorComponent,Is.GreaterThan(.65f),"Border must remain bright independently of terrain texture");
                    Assert.That(r.GetPosition(0).y,Is.GreaterThan(r.GetComponentInParent<TileView>().GetComponent<Renderer>().bounds.max.y));
                }
                var destination=reachable.First(p=>p!=origin);
                director.Board.ShowPath(director.Session.Grid.Path(active,destination));
                var path=Object.FindObjectsByType<LineRenderer>().Single(r=>r.name=="Movement Path");
                Assert.That(path.positionCount,Is.GreaterThan(1));
                Click("취소");yield return null;
                Assert.That(borders.All(r=>!r.enabled),Is.True);Assert.That(path.positionCount,Is.Zero);
                Click("Move / 이동");yield return null;
                Assert.That(Object.FindObjectsByType<LineRenderer>().Count(r=>r.name.StartsWith("Range border ")),Is.EqualTo(borders.Length),"Reuse existing border renderers");
                Click("취소");Click("Attack / 공격");yield return null;
                var inRange=director.Session.Grid.Tiles.Keys.Where(p=>director.Session.Resolver.InRange(active,director.SelectedSkill,p)).ToArray();
                CollectionAssert.AreEquivalent(inRange,Object.FindObjectsByType<LineRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Range border ")).Select(r=>r.GetComponentInParent<TileView>().Coordinate).ToArray());
                director.Board.ShowArea(inRange.First(),0);yield return null;
                Assert.That(Object.FindObjectsByType<LineRenderer>().Any(r=>r.enabled&&r.name.StartsWith("Range border ")&&r.startWidth>.06f),Is.True,"Selected area gets a stronger gold border");
                for(int turn=0;turn<4;turn++){Click("우회전");yield return null;}
                Click("확대 +");Click("초기화");
                Assert.That(active.Position,Is.EqualTo(origin));CollectionAssert.AreEqual(hp,director.Session.Units.Select(u=>u.CurrentHP).ToArray());
                var scenery=Object.FindAnyObjectByType<PixelBattlefield>();
                var materials=scenery.GetComponentsInChildren<Renderer>().Select(r=>r.sharedMaterial).Where(m=>m!=director.Board.HighlightMaterial&&m!=director.Board.TileMaterial&&m!=director.Board.SpriteMaterial).Distinct().ToArray();
                Assert.That(materials.Length,Is.GreaterThan(0));
                director.Restart();yield return null;yield return null;
                Assert.That(Object.FindObjectsByType<PixelBattlefield>(),Is.Empty);
                Assert.That(Object.FindObjectsByType<LineRenderer>().Any(r=>r.name.StartsWith("Range border ")),Is.False);
                Assert.That(materials.All(m=>m==null),Is.True,"All generated scenery materials must be destroyed on restart");
            }
        }
        [UnityTest] public IEnumerator PixelRosterAndSceneryPreserveSixChapterInteraction()
        {
            Assert.That(director.Catalog.Enemies.Length,Is.EqualTo(10));
            foreach(var character in director.Catalog.Characters.Concat(director.Catalog.Enemies))
            {
                foreach(Facing facing in System.Enum.GetValues(typeof(Facing)))
                {
                    var sprite=character.Sprites.Get(facing);
                    Assert.That(sprite,Is.Not.Null,character.Id);
                    Assert.That(sprite.texture.filterMode,Is.EqualTo(FilterMode.Point),character.Id);
                    Assert.That(sprite.texture.mipmapCount,Is.EqualTo(1));
                    Assert.That(sprite.rect.width,Is.GreaterThan(50));
                    Assert.That(sprite.bounds.size.y,Is.InRange(.45f,1.7f));
                    Assert.That(character.Poses.Walk.Frames.All(f=>f.Get(facing)!=null),Is.True);
                    Assert.That(character.Poses.Dead.Frames.All(f=>f.Get(facing)!=null),Is.True);
                }
                if(director.Catalog.Characters.Contains(character))Assert.That(character.Sprites.Front.texture.name,Is.EqualTo(character.Id+"-hq"));
            }
            director.TrainingMode=false;director.Campaign=new CampaignSave();director.PersistCampaign=_=>true;
            foreach(int s in Enumerable.Range(0,6))director.Campaign.StoryProgress.Add(CampaignStages.Id(s));
            for(int stage=0;stage<6;stage++)
            {
                director.SelectedStage=stage;director.BeginBattle();yield return null;
                var enemies=director.Session.Units.Where(u=>u.Team==Team.Enemy).ToArray();
                for(int slot=0;slot<4;slot++)Assert.That(enemies[slot].Data.Id,Is.EqualTo(CampaignEnemies.Id(stage,slot)));
                var scenery=Object.FindAnyObjectByType<PixelBattlefield>();Assert.That(scenery,Is.Not.Null);
                Assert.That(scenery.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name.StartsWith("Authored scenery")),Is.GreaterThan(0));
                Assert.That(scenery.GetComponentsInChildren<Collider>().All(c=>c.GetComponent<TileView>()!=null),Is.True,"Only gameplay tiles may intercept picking");
                for(int rotation=0;rotation<4;rotation++)
                {
                    director.Board.RotateCamera(90);yield return null;
                    foreach(var tile in director.Session.Grid.Tiles.Values.Where(t=>t.Walkable))
                    {
                        // Probe the unchanged top surface just inside the gameplay collider.
                        var top=new Vector3(tile.Coordinate.x,tile.Height*director.Catalog.Rules.TileHeight-.001f,tile.Coordinate.y);
                        var point=director.Board.BattleCamera.WorldToScreenPoint(top);
                        Assert.That(director.Board.Pick(point,out var picked),Is.True);
                        Assert.That(picked,Is.EqualTo(tile.Coordinate),"Scenery must not block "+stage+"/"+tile.Coordinate);
                    }
                }
                director.Restart();yield return null;
            }
        }
    }
}
