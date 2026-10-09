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
            var motion=Object.FindObjectsByType<CharacterMotion>().Single(x=>x.transform.parent.name==unit.Data.DisplayName);
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
        [UnityTest] public IEnumerator EffectOverlapIsBoundedAndMuteAndStopResetPlayback()
        {
            var audio=director.Audio;var original=audio.Library;
            var library=ScriptableObject.CreateInstance<AudioLibrary>();
            var clip=AudioClip.Create("overlap-test",44100,1,44100,false);
            library.Entries=new[]{"a","b","c","d","e"}.Select(id=>new AudioEntry{Id=id,Clip=clip}).ToArray();
            try
            {
                audio.StopAll();audio.Library=library;
                var music=audio.GetComponent<AudioSource>();var previous=music.clip;
                audio.PlayEffect("a");audio.PlayEffect("a");
                Assert.That(audio.ActiveEffectVoices,Is.EqualTo(1),"Same-frame impacts must not stack identical sounds.");
                foreach(var id in new[]{"b","c","d","e"})audio.PlayEffect(id);
                Assert.That(audio.ActiveEffectVoices,Is.EqualTo(4));Assert.That(audio.LastEffectGain,Is.EqualTo(.5f).Within(.001f));
                Assert.That(music.clip,Is.EqualTo(previous));
                audio.StopAll();Assert.That(audio.ActiveEffectVoices,Is.Zero);
                audio.SetVolumes(.28f,0);audio.PlayEffect("a");Assert.That(audio.ActiveEffectVoices,Is.Zero);
                audio.SetVolumes(.28f,.45f);audio.PlayEffect("a");Assert.That(audio.ActiveEffectVoices,Is.EqualTo(1));
                Time.timeScale=0;
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(audio.ActiveEffectVoices,Is.Zero,"Voice lifetime must follow audio, independently of battle speed.");
            }
            finally {Time.timeScale=1;audio.StopAll();audio.Library=original;Object.Destroy(library);Object.Destroy(clip);}
        }
        [UnityTest] public IEnumerator ImpactNumbersHoldContrastAndRestartRemovesBacking()
        {
            director.BeginBattle();yield return null;
            var unit=director.Session.Units.First();var before=director.Board.CaptureHealth();unit.CurrentHP-=10;
            int health=unit.CurrentHP;
            director.Board.PresentImpact(unit,unit.Data.BasicAttack,unit.Position,before);
            var feedback=GameObject.Find("HP feedback");Assert.That(feedback,Is.Not.Null);
            var label=feedback.GetComponent<TMPro.TextMeshPro>();
            var backing=feedback.transform.Find("Feedback backing");Assert.That(backing,Is.Not.Null);
            Assert.That(backing.GetComponent<Renderer>().sortingOrder,Is.LessThan(label.sortingOrder));
            yield return new WaitForSeconds(.2f);
            Assert.That(label.alpha,Is.EqualTo(1).Within(.001f));Assert.That(unit.CurrentHP,Is.EqualTo(health));
            director.Board.RotateCamera(90);yield return null;
            var camera=director.Board.BattleCamera;
            var basePosition=director.Session.Grid[unit.Position].WorldPosition(director.Catalog.Rules.TileHeight);
            var offset=feedback.transform.position-basePosition;
            Assert.That(Vector3.Dot(offset,camera.transform.right),Is.EqualTo(0).Within(.01f));
            Assert.That(Vector3.Dot(offset,camera.transform.up),Is.GreaterThan(1.7f));
            director.Restart();yield return null;yield return null;
            Assert.That(GameObject.Find("HP feedback"),Is.Null);Assert.That(GameObject.Find("Feedback backing"),Is.Null);
        }
    }
}
