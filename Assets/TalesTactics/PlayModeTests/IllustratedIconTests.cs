using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator IllustratedAtlasRegionsAreDistinctAndInsideTextures()
        {
            string[] keys="deployed selected done locked armor accessory move attack skill guard wait endturn balance swift protector focus heal undo single line cone area danger direction previous next close return empty gold rescue linked portal contract hazard collapse summon core".Split(' ');
            var regions=keys.Select(k=>IllustratedIcons.Get(k)).ToArray();Assert.That(regions.All(r=>r!=null&&r.Texture!=null),Is.True);
            foreach(var r in regions){Assert.That(r.width,Is.GreaterThan(70));Assert.That(r.height,Is.GreaterThan(70));Assert.That(r.UV.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(r.UV.yMin,Is.GreaterThanOrEqualTo(0));Assert.That(r.UV.xMax,Is.LessThanOrEqualTo(1));Assert.That(r.UV.yMax,Is.LessThanOrEqualTo(1));}
            foreach(string key in "deployed done move attack direction previous next locked".Split(' '))Assert.That(IllustratedIcons.Get(key,true).atlas,Is.EqualTo("CompactIcons"));
            Assert.That(regions.Select(r=>r.atlas+":"+r.x+":"+r.y).Distinct().Count(),Is.EqualTo(keys.Length));yield return null;
        }
        [UnityTest] public IEnumerator IllustratedSelectionAndCompletionHaveDifferentArtWithoutInputTargets()
        {
            director.Hud.ShowDeployment();yield return new WaitForEndOfFrame();
            Assert.That(director.Hud.GetComponentsInChildren<TacticalIcon>().Any(i=>i.Kind=="deployed"),Is.True);
            Assert.That(director.Hud.GetComponentsInChildren<IconSelectionCorners>(),Is.Not.Empty);
            director.BeginBattle();yield return null;director.StopAllCoroutines();var b=director.Session;var team=(TeamTurnScheduler)b.Scheduler;
            team.Complete(b.Active);b.Select(team.Preview(b.Units).First());director.SetState(new CommandState(director));yield return new WaitForEndOfFrame();
            Assert.That(director.Hud.GetComponentsInChildren<TacticalIcon>().Any(i=>i.Kind=="done"),Is.True);
            foreach(var icon in director.Hud.GetComponentsInChildren<TacticalIcon>()){Assert.That(icon.Region,Is.Not.Null,icon.Kind);Assert.That(icon.mainTexture,Is.Not.Null);Assert.That(icon.raycastTarget,Is.False);}
        }
        [UnityTest] public IEnumerator IllustratedFieldMarkersFollowCameraVisibilityAndHazardLifecycle()
        {
            director.Catalog.CrossoverReady=true;director.TrainingMode=false;director.SelectedStage=CrossoverCampaign.FirstStage+2;
            director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(1));director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(2));
            director.BeginBattle();yield return null;director.StopAllCoroutines();var b=director.Session;
            var cell=b.Grid.Tiles.Values.First().Coordinate;
            b.Crossover.State.Hazards.Add(new CrossoverHazard{X=cell.x,Y=cell.y,Kind=BossMechanic.Collapse});yield return new WaitForEndOfFrame();
            var collapse=director.Board.GetComponentsInChildren<FieldIconBillboard>().Single(x=>x.name=="Field icon - collapse");
            for(int i=0;i<4;i++){director.Board.RotateCamera(90);yield return new WaitForEndOfFrame();Assert.That(Quaternion.Angle(collapse.transform.rotation,director.Board.BattleCamera.transform.rotation),Is.LessThan(.01f));}
            Assert.That(collapse.GetComponent<Collider>(),Is.Null);
            director.Hud.ShowSystemMenu();yield return new WaitForEndOfFrame();Assert.That(collapse.GetComponent<SpriteRenderer>().enabled,Is.False);
            director.Hud.CloseSystemMenu();b.Crossover.State.Hazards[0].Triggered=true;yield return new WaitForEndOfFrame();Assert.That(collapse.gameObject.activeSelf,Is.False);
            Assert.That(director.Board.GetComponentsInChildren<FieldIconBillboard>().Any(x=>x.name=="Field icon - hazard"),Is.True);
            b.Crossover.State.Hazards.Clear();yield return new WaitForEndOfFrame();Assert.That(director.Board.GetComponentsInChildren<FieldIconBillboard>().Any(x=>x.name=="Field icon - hazard"),Is.False);
            b.Crossover.State.Nodes=1;yield return new WaitForEndOfFrame();
            var markers=director.Board.GetComponentsInChildren<FieldIconBillboard>();Assert.That(markers.Count(x=>x.name=="Field icon - linked"),Is.EqualTo(1));Assert.That(markers.Count(x=>x.name=="Field icon - rescue"),Is.EqualTo(2));Assert.That(markers.Count(x=>x.name=="Field icon - portal"),Is.EqualTo(1));
            var boss=b.Crossover.Boss;boss.IntentPhase=1;yield return new WaitForEndOfFrame();
            var warning=director.Board.GetComponentsInChildren<SpriteRenderer>().Single(x=>x.sprite!=null&&x.sprite.name=="danger:-1");
            boss.IntentPhase=2;yield return new WaitForEndOfFrame();Assert.That(warning.sprite.name,Is.EqualTo("core:-1"));
            boss.IntentPhase=0;yield return new WaitForEndOfFrame();Assert.That(warning.gameObject.activeSelf,Is.False);

        }
    }
}
