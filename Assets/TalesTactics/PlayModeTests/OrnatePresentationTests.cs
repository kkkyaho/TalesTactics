using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator OrnateFramesHaveSlicedArtAndDoNotInterceptButtons()
        {
            director.Hud.ShowIntermissionHub();yield return null;
            var frames=director.Hud.GetComponentsInChildren<OrnatePanel>();Assert.That(frames.Length,Is.GreaterThan(0));
            foreach(var frame in frames){var image=frame.GetComponent<Image>();Assert.That(image.sprite,Is.Not.Null);Assert.That(image.type,Is.EqualTo(Image.Type.Sliced));Assert.That(image.sprite.border.x,Is.GreaterThan(0));}
            foreach(var trim in director.Hud.GetComponentsInChildren<OrnateButtonGraphic>())Assert.That(trim.raycastTarget,Is.False);
            var button=director.Hud.GetComponentsInChildren<Button>().First(b=>b.IsInteractable());EventSystem.current.SetSelectedGameObject(button.gameObject);yield return null;
            Assert.That(button.GetComponentInChildren<OrnateButtonGraphic>().Selected,Is.True);
            button.interactable=false;yield return null;Assert.That(button.GetComponentInChildren<OrnateButtonGraphic>().Disabled,Is.True);
        }
        [UnityTest] public IEnumerator LivingTerrainRebuildKeepsRulesRandomAndReleasesCanopies()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();
            var session=director.Session;string random=JsonUtility.ToJson(Random.state);
            var tiles=session.Grid.Tiles.Values.Select(t=>t.Coordinate+":"+t.Height+":"+t.Walkable+":"+t.MovementCost).ToArray();
            director.Board.Build(session);yield return null;
            var scenery=director.Board.GetComponentInChildren<PixelBattlefield>();Assert.That(scenery.CanopyCount,Is.GreaterThan(10));
            Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));Assert.That(session.Grid.Tiles.Values.Select(t=>t.Coordinate+":"+t.Height+":"+t.Walkable+":"+t.MovementCost),Is.EqualTo(tiles));
            var trees=scenery.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("Forest canopy")).ToArray();var owned=trees.Select(r=>r.sprite).Distinct().ToArray();
            var details=scenery.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Foliage detail")).ToArray();
            var detailMeshes=details.Select(f=>f.sharedMesh).Distinct().ToArray();var detailMaterial=details.First().GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(detailMeshes.Length,Is.GreaterThan(0));
            foreach(var mesh in detailMeshes){var uv=mesh.uv;Assert.That(uv.Max(v=>v.x)-uv.Min(v=>v.x),Is.LessThan(.25f));Assert.That(uv.Max(v=>v.y)-uv.Min(v=>v.y),Is.LessThan(.25f));}
            Assert.That(trees.All(t=>t.GetComponent<Collider>()==null),Is.True);
            Assert.That(details.All(t=>t.GetComponent<Collider>()==null),Is.True);
            director.Restart();yield return null;yield return null;Assert.That(owned.All(s=>s==null),Is.True);Assert.That(detailMeshes.All(m=>m==null),Is.True);Assert.That(detailMaterial==null,Is.True);
        }
    }
}
