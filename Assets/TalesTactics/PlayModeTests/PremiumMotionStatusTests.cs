using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator PremiumStatusTabsAndCyclingNeverChangeBattleOrResources()
        {
            director.BeginBattle();yield return null;
            var active=director.Session.Active;var state=director.State;var position=active.Position;
            int hp=active.CurrentHP,mp=active.CurrentMP;var save=JsonUtility.ToJson(director.Campaign);
            active.AddStatus(StatusKind.Song,3);active.AddStatus(StatusKind.Root,2);
            director.Hud.ShowUnitDetails(active);yield return null;
            Assert.That(director.Hud.CharacterStatusOpen,Is.True);
            var normalSize=((RectTransform)director.Hud.transform.Find("UnitDetails")).sizeDelta;
            director.Preferences.TextScale=1.3f;director.Hud.ShowUnitDetails(active);yield return null;
            Assert.That(((RectTransform)director.Hud.transform.Find("UnitDetails")).sizeDelta.x,Is.GreaterThan(normalSize.x));
            Assert.That(director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.name=="StatusValueSTR").fontSizeMax,Is.EqualTo(26).Within(.01f));
            director.Preferences.TextScale=1;director.Hud.ShowUnitDetails(active);yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("성가 · 3턴")),Is.True);
            foreach(string tab in new[]{"상태 기술","상태 장비","상태 능력"})
            {
                Click(tab);yield return null;
                Assert.That(director.Session.Active,Is.SameAs(active));Assert.That(director.State,Is.SameAs(state));
            }
            Click("다음 상태 캐릭터");yield return null;
            Assert.That(director.Hud.DisplayedStatusUnit,Is.Not.SameAs(active));
            Assert.That(director.Session.Active,Is.SameAs(active));
            Click("이전 상태 캐릭터");yield return null;
            Assert.That(director.Hud.DisplayedStatusUnit,Is.SameAs(active));
            Click("모델 회전");yield return null;Assert.That(active.Facing,Is.EqualTo(Facing.Front));
            director.GetComponent<GamepadPointer>().Cancel();yield return null;
            Assert.That(director.Hud.InputModalOpen,Is.False);
            Assert.That(director.Hud.transform.Find("StatusBackdrop").gameObject.activeSelf,Is.False);
            Assert.That(active.CurrentHP,Is.EqualTo(hp));Assert.That(active.CurrentMP,Is.EqualTo(mp));
            Assert.That(active.Position,Is.EqualTo(position));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(save));
        }
        [UnityTest] public IEnumerator PremiumStatusSupportsEveryHeroEnemyAndAllStatePages()
        {
            director.BeginBattle();yield return null;
            var all=director.Catalog.Characters.Concat(director.Catalog.Enemies);
            foreach(var data in all)
            {
                var unit=new UnitRuntime(data,CharacterVisualSizing.IsHero(data)?Team.Player:Team.Enemy,director.Catalog.Rules,25);
                foreach(StatusKind kind in System.Enum.GetValues(typeof(StatusKind)))if(kind!=StatusKind.None)unit.AddStatus(kind,2);
                if(unit.Team==Team.Enemy){unit.IntentPhase=1;unit.IntentSkill=data.BasicAttack.Id;unit.IntentAim=new Vector2Int(3,4);}
                director.Hud.ShowUnitDetails(unit);yield return null;
                if(unit.Team==Team.Enemy)Assert.That(director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains(EnemyTactics.Describe(unit))),Is.True);
                for(int page=0;page<(unit.Statuses.Count+4)/2;page++)
                {
                    Canvas.ForceUpdateCanvases();
                    foreach(var text in director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<TMPro.TMP_Text>())
                    {text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,data.Id+": "+text.text);}
                    var next=director.Hud.GetComponentsInChildren<Button>().Single(b=>b.name=="상태 효과 다음");
                    if(next.interactable)next.onClick.Invoke();yield return null;
                }
                foreach(var tab in new[]{"상태 기술","상태 장비"})
                {
                    Click(tab);yield return null;
                    for(int page=0;page<10;page++)
                    {
                        Canvas.ForceUpdateCanvases();
                        foreach(var text in director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<TMPro.TMP_Text>())
                        {text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,data.Id+": "+text.text);}
                        var next=director.Hud.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="상태 다음 기술");
                        if(next==null||!next.interactable)break;
                        next.onClick.Invoke();yield return null;
                    }
                }
                director.Hud.CloseUnitDetails();yield return null;
            }
        }
        [UnityTest] public IEnumerator PremiumSkillAndUltimateFramesAreUniqueAndNeverReleaseEarly()
        {
            var camera=director.Board.BattleCamera;
            foreach(var data in director.Catalog.Characters)
            {
                foreach(var clip in new[]{data.Poses.Skill,data.Poses.Ultimate})
                    foreach(Facing facing in System.Enum.GetValues(typeof(Facing)))
                    {
                        Assert.That(clip.Frames.All(f=>f.Get(facing)!=null),Is.True,data.Id);
                        Assert.That(clip.Frames.Select(f=>f.Get(facing)).Distinct().Count(),Is.EqualTo(clip.Frames.Length),data.Id);
                        Assert.That(clip.SamplePhase(facing,100,.2f,false),Is.SameAs(clip.Frames[clip.ReleaseFrame-1].Get(facing)));
                    }
                var unit=new UnitRuntime(data,Team.Player,director.Catalog.Rules);
                var go=new GameObject("Premium motion test");var renderer=go.AddComponent<SpriteRenderer>();var motion=go.AddComponent<CharacterMotion>();motion.Initialize(renderer,unit,camera);
                motion.BeginSkill(data.UltimateSkill,.05f,.85f);yield return new WaitForSeconds(.1f);yield return new WaitForEndOfFrame();
                var view=CharacterMotion.ViewFacing(unit.Facing,camera.transform.rotation);
                Assert.That(renderer.sprite,Is.SameAs(data.Poses.Ultimate.Frames[data.Poses.Ultimate.ReleaseFrame-1].Get(view)));
                float clock=Time.timeScale;motion.ReleaseSkill();yield return new WaitForEndOfFrame();
                Assert.That(renderer.sprite,Is.SameAs(data.Poses.Ultimate.Frames[data.Poses.Ultimate.ReleaseFrame].Get(view)));
                Assert.That(Time.timeScale,Is.EqualTo(clock));
                motion.Set(AnimationKind.Guard);yield return new WaitForEndOfFrame();Assert.That(motion.Presenting,Is.False);
                Object.Destroy(go);
            }
        }
    }
}
