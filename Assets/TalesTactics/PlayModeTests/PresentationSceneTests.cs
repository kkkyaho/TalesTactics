using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ArtDirectionsMotionAndCameraRotationRemainConnected()
        {
            foreach(var data in director.Catalog.Characters)
            {
                Assert.That(data.Portrait,Is.Not.Null,data.Id);
                foreach(Facing facing in System.Enum.GetValues(typeof(Facing)))Assert.That(data.Sprites.Get(facing),Is.Not.Null,data.Id+facing);
            }
            director.BeginBattle();yield return null;
            var unit=director.Session.Units.First();
            var motion=Object.FindObjectsByType<CharacterMotion>(FindObjectsSortMode.None).Single(x=>x.transform.parent.name==unit.Data.DisplayName);
            foreach(AnimationKind action in System.Enum.GetValues(typeof(AnimationKind)))
            {director.Board.SetAnimation(unit,action);yield return null;Assert.That(motion.Action,Is.EqualTo(action));}
            director.Board.SetAnimation(unit,AnimationKind.Idle);
            foreach(Facing facing in System.Enum.GetValues(typeof(Facing)))
            {
                unit.Facing=facing;director.Board.Sync();director.Board.RotateCamera(90);yield return null;
                var expected=CharacterMotion.ViewFacing(facing,director.Board.BattleCamera.transform.rotation);
                Assert.That(motion.GetComponent<SpriteRenderer>().sprite,Is.EqualTo(unit.Data.Sprites.Get(expected)));
                var bar=motion.transform.parent.Find("HP");
                var screenHead=director.Board.BattleCamera.WorldToScreenPoint(motion.transform.position+director.Board.BattleCamera.transform.up*1.3f);
                var screenBar=director.Board.BattleCamera.WorldToScreenPoint(bar.position);
                Assert.That(screenBar.y,Is.GreaterThan(screenHead.y),"HP bar must stay above the billboard head after camera rotation.");
            }
        }
        [UnityTest] public IEnumerator EffectsExpireAndRestartCleansPresentation()
        {
            director.BeginBattle();yield return null;
            var unit=director.Session.Units.First();var before=director.Board.CaptureHealth();unit.CurrentHP-=10;
            director.Board.PresentImpact(unit,unit.Data.BasicAttack,unit.Position,before);yield return null;
            Assert.That(GameObject.Find("HP feedback"),Is.Not.Null);
            yield return new WaitForSeconds(0.8f);
            Assert.That(GameObject.Find("HP feedback"),Is.Null);Assert.That(GameObject.Find("Impact burst"),Is.Null);
            director.Board.ShowTimingSpin(unit,0.5f);
            director.Board.PresentImpact(unit,unit.Data.BasicAttack,unit.Position,before);
            director.Restart();yield return null;yield return null;
            Assert.That(GameObject.Find("Timing arc"),Is.Null);Assert.That(GameObject.Find("Skill trail"),Is.Null);
        }
        [UnityTest] public IEnumerator EffectsAudioDoesNotReplaceMusicSource()
        {
            foreach(var id in new[]{"swing","hit","heal","cast"})Assert.That(director.Audio.Library.Entries.Single(x=>x.Id==id).Clip,Is.Not.Null);
            var source=director.Audio.GetComponent<AudioSource>();var previous=source.clip;
            director.Audio.PlayEffect("hit");yield return null;
            Assert.That(source.clip,Is.EqualTo(previous));Assert.That(director.Audio.GetComponents<AudioSource>().Length,Is.EqualTo(2));
            director.Audio.StopAll();Assert.That(director.Audio.GetComponents<AudioSource>().All(x=>!x.isPlaying),Is.True);
        }
    }
}
