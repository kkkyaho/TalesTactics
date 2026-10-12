using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ArchitecturalTerrainPreservesSixMapsAndRotatedPicking()
        {
            director.TrainingMode=false;director.Campaign=new CampaignSave();director.PersistCampaign=_=>true;
            for(int stage=0;stage<6;stage++)director.Campaign.StoryProgress.Add(CampaignStages.Id(stage));
            for(int stage=0;stage<6;stage++)
            {
                director.SelectedStage=stage;director.BeginBattle();if(director.StoryActive)director.FinishStory();director.StopAllCoroutines();yield return null;
                var session=director.Session;var expected=CampaignContent.Map(stage);var scenery=director.Board.GetComponentInChildren<PixelBattlefield>();
                Assert.That(scenery.Chapter,Is.EqualTo(stage));Assert.That(scenery.DecorationMeshCount,Is.InRange(2,17));
                Assert.That(session.Grid.Tiles.Count,Is.EqualTo(expected.Tiles.Count));
                foreach(var tile in session.Grid.Tiles.Values)
                {
                    var original=expected[tile.Coordinate];Assert.That(tile.Height,Is.EqualTo(original.Height));Assert.That(tile.Terrain,Is.EqualTo(original.Terrain));
                    Assert.That(tile.MovementCost,Is.EqualTo(original.MovementCost));Assert.That(tile.Walkable,Is.EqualTo(original.Walkable));
                }
                Assert.That(scenery.GetComponentsInChildren<Collider>().All(c=>c.GetComponent<TileView>()!=null),Is.True,"Decorations must never intercept picks");
                var state=JsonUtility.ToJson(Random.state);
                for(int rotation=0;rotation<4;rotation++)
                {
                    director.Board.RotateCamera(90);yield return new WaitForEndOfFrame();
                    foreach(var tile in session.Grid.Tiles.Values.Where(t=>t.Walkable))
                    {
                        var screen=director.Board.BattleCamera.WorldToScreenPoint(tile.WorldPosition(session.Rules.TileHeight));
                        Assert.That(director.Board.Pick(screen,out var picked),Is.True,$"stage {stage} rotation {rotation} tile {tile.Coordinate}");
                        Assert.That(picked,Is.EqualTo(tile.Coordinate));
                    }
                }
                Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(state),"Scenery must not consume gameplay RNG");
                director.Restart();yield return null;
            }
        }
        [UnityTest] public IEnumerator ArchitecturalTerrainReleasesOwnedMeshesMaterialsAndSpritesOnRestart()
        {
            director.BeginBattle();yield return null;
            var scenery=director.Board.GetComponentInChildren<PixelBattlefield>();
            var meshes=scenery.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Architecture")||f.name=="Panorama surface").Select(f=>f.sharedMesh).ToArray();
            var materials=scenery.GetComponentsInChildren<Renderer>().Select(r=>r.sharedMaterial).Where(m=>m!=null&&(m.name.StartsWith("Architectural terrain")||m.name.StartsWith("Chapter panorama")||m.name=="Continuous chapter pavement"||m.name=="Carved stone facade")).Distinct().ToArray();
            var sprites=scenery.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("Terrain accent")||r.name.StartsWith("Architectural detail")).Select(r=>r.sprite).Distinct().ToArray();
            Assert.That(meshes.Length,Is.GreaterThan(1));Assert.That(materials.Length,Is.GreaterThan(1));Assert.That(sprites.Length,Is.GreaterThan(1));
            director.Restart();yield return null;yield return null;
            Assert.That(meshes.All(m=>m==null),Is.True);Assert.That(materials.All(m=>m==null),Is.True);Assert.That(sprites.All(s=>s==null),Is.True);
        }
    }
}
