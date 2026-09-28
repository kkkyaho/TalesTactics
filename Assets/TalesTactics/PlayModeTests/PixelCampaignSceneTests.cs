using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
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
