using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator SkillCueUsesFixedViewportAndNeverDuplicatesUltimate()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();
            var caster=director.Session.Active;var skill=Object.Instantiate(caster.Data.Skills[0]);
            string before=string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))));
            string random=JsonUtility.ToJson(Random.state);
            try
            {
                skill.DisplayName="빛과 어둠을 가르는 최후의 심판";skill.Animation=AnimationKind.Cast;
                skill.Presentation=new SkillPresentation{Windup=1.5f};
                director.Board.BeginSkill(caster,skill,caster.Position,4);yield return new WaitForEndOfFrame();
                Assert.That(GameObject.Find("Skill name"),Is.Null);Assert.That(GameObject.Find("Skill name backing"),Is.Null);
                var title=GameObject.Find("Battle skill title").GetComponent<TMP_Text>();title.ForceMeshUpdate();
                Assert.That(title.text,Is.EqualTo(skill.DisplayName));Assert.That(title.isTextOverflowing,Is.False);
                Assert.That(director.Hud.BattleSkillCueVisible,Is.True);
                var bounds=director.Hud.BattleSkillCueScreenBounds;AssertClarityInside(bounds,CombatLabelPlacement.Viewport(director.Hud));
                director.Board.RotateCamera(90);yield return new WaitForEndOfFrame();
                Assert.That(Vector2.Distance(bounds.center,director.Hud.BattleSkillCueScreenBounds.center),Is.LessThan(.1f),"Skill names do not move with head projection or rotation.");
                Assert.That(title.GetComponentsInParent<CanvasGroup>().All(g=>!g.blocksRaycasts&&!g.interactable),Is.True);
                director.Board.BeginSkill(caster,caster.Data.UltimateSkill,caster.Position,1);yield return new WaitForEndOfFrame();
                Assert.That(director.Hud.BattleSkillCueVisible,Is.False);Assert.That(GameObject.Find("UltimateSkillCue"),Is.Not.Null);
                Assert.That(GameObject.Find("Skill name"),Is.Null);
                Assert.That(string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u)))),Is.EqualTo(before));
                Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));
                director.Restart();yield return null;yield return null;
                Assert.That(GameObject.Find("BattleSkillCue"),Is.Null);Assert.That(GameObject.Find("UltimateSkillCue"),Is.Null);
            }
            finally{Object.Destroy(skill);}
        }
        [UnityTest] public IEnumerator SkillCueWaitsBehindModalAndRetainsReadableFastBattleDwell()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();
            var caster=director.Session.Active;var skill=caster.Data.Skills.First(s=>s.Animation!=AnimationKind.Attack&&!s.IsUltimate);
            foreach(int speed in new[]{1,2,4})
            {
                director.Hud.ShowBattleSkillCue(caster,skill,.12f/speed);yield return new WaitForEndOfFrame();
                director.Hud.ShowHelp();
                Assert.That(director.Hud.HelpOpen,Is.True,"Establish the modal before checking its rendered suppression.");
                // A coroutine resumed by yield null runs before the banner's LateUpdate.
                // Inspect the rendered frame, after the presentation layer has applied modal visibility.
                yield return new WaitForEndOfFrame();
                Assert.That(director.Hud.BattleSkillCueVisible,Is.False);
                yield return new WaitForSeconds(.3f);director.Hud.CloseHelp();yield return new WaitForEndOfFrame();
                Assert.That(director.Hud.BattleSkillCueVisible,Is.True,"Opening help must not consume the readable cue lifetime.");
                float originalScale=Time.timeScale;
                try{Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);Assert.That(director.Hud.BattleSkillCueVisible,Is.True);}
                finally{Time.timeScale=originalScale;}
                yield return new WaitForSeconds(.35f);Assert.That(director.Hud.BattleSkillCueVisible,Is.True,"Fast enemy actions retain a minimum reading interval without delaying the turn.");
                yield return new WaitForSeconds(.7f);Assert.That(director.Hud.BattleSkillCueVisible,Is.False);
            }
        }
        [UnityTest] public IEnumerator CrowdedSixAlliesKeepNumbersAndFocusedHealthInsideViewport()
        {
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,2,3,4,5});
            director.BeginBattle();yield return null;director.StopAllCoroutines();
            Assert.That(director.Session.Units.Count(u=>u.Team==Team.Player),Is.EqualTo(6));
            var visualUnits=(Dictionary<UnitRuntime,Transform>)typeof(BoardView).GetField("units",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(director.Board);
            var tile=director.Session.Grid.Tiles.Values.OrderBy(t=>Vector2Int.Distance(t.Coordinate,new Vector2Int(4,4))).First();
            var center=tile.WorldPosition(director.Catalog.Rules.TileHeight);int i=0;
            foreach(var pair in visualUnits){pair.Value.position=center+new Vector3((i%3)*.12f,0,(i/3)*.12f);i++;}
            string before=string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))));
            var units=director.Session.Units.Where(u=>u.Alive&&!u.CrossoverReserve).ToArray();var feedback=director.Hud.CombatFeedback;
            float originalScale=Time.timeScale;
            try
            {
                Time.timeScale=0;director.Preferences.TextScale=1.3f;
                foreach(var unit in units)feedback.Number(unit,-123,false);
                feedback.Number(units[0],45,false);feedback.Number(units[0],-10,true);
                for(int rotation=0;rotation<4;rotation++)
                {
                    yield return new WaitForEndOfFrame();var safe=CombatLabelPlacement.Viewport(director.Hud);
                    var occupied=new List<Rect>(feedback.NumberScreenBounds);
                    Assert.That(occupied.Count,Is.EqualTo(units.Length+2));
                    var displays=Object.FindObjectsByType<UnitHealthDisplay>(FindObjectsSortMode.None);
                    Assert.That(displays.Single(d=>d.Unit==director.Session.Active).Bar.enabled,Is.True);
                    Assert.That(displays.Single(d=>d.Unit==director.Session.Active).Detailed,Is.False,"Current impact values take precedence over duplicated HP detail.");
                    occupied.AddRange(displays.Where(d=>d.Bar.enabled).Select(d=>d.ScreenBounds));
                    for(int a=0;a<occupied.Count;a++)
                    {
                        AssertClarityInside(occupied[a],safe);
                        for(int b=a+1;b<occupied.Count;b++)Assert.That(occupied[a].Overlaps(occupied[b]),Is.False,"Damage/HP collision after camera rotation "+rotation+": "+a+" / "+b);
                    }
                    director.Board.RotateCamera(90);
                }
                director.Hud.ShowHelp();yield return new WaitForEndOfFrame();
                Assert.That(Object.FindObjectsByType<UnitHealthDisplay>(FindObjectsSortMode.None).All(d=>!d.Bar.enabled),Is.True);
                Assert.That(feedback.NumberScreenBounds.Count,Is.Zero);director.Hud.CloseHelp();
                feedback.Clear();director.WaitCommand();
                Assert.That(director.State,Is.InstanceOf<FacingSelectionState>());
                yield return new WaitForEndOfFrame();
                var facingPanel=(RectTransform)director.Hud.transform.Find("Commands");
                Assert.That(facingPanel.GetComponentsInChildren<FacingArrowGraphic>().Length,Is.EqualTo(4));
                var arrowRegion=CombatLabelPlacement.ScreenRect(facingPanel);
                foreach(var display in Object.FindObjectsByType<UnitHealthDisplay>(FindObjectsSortMode.None).Where(d=>d.Bar.enabled))
                    Assert.That(display.ScreenBounds.Overlaps(arrowRegion),Is.False,"Crowded HP bars must not cover the actor's four direction choices.");
            }
            finally{Time.timeScale=originalScale;director.Preferences.TextScale=1;}
            Assert.That(string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u)))),Is.EqualTo(before),"Only presentation transforms changed.");
            director.Restart();yield return null;Assert.That(feedback.NumberCount,Is.Zero);Assert.That(feedback.NumberScreenBounds.Count,Is.Zero);
        }
        static void AssertClarityInside(Rect child,Rect safe)
        {
            Assert.That(child.xMin,Is.GreaterThanOrEqualTo(safe.xMin-.2f));Assert.That(child.yMin,Is.GreaterThanOrEqualTo(safe.yMin-.2f));
            Assert.That(child.xMax,Is.LessThanOrEqualTo(safe.xMax+.2f));Assert.That(child.yMax,Is.LessThanOrEqualTo(safe.yMax+.2f));
        }
    }
}
