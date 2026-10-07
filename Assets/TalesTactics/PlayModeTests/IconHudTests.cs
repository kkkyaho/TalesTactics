using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator CompactSkillsExposeOnlyUsableSkillsWithoutSpendingCosts()
        {
            director.BeginBattle();yield return null;
            var unit=director.Session.Active;int hp=unit.CurrentHP,mp=unit.CurrentMP;
            Click("Skill / 스킬");yield return null;
            var found=new System.Collections.Generic.HashSet<string>();
            for(int page=0;page<4;page++)
            {
                var cards=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.name.Contains(" · MP")).ToArray();
                Assert.That(cards.Length,Is.InRange(1,4));
                Assert.That(((RectTransform)director.Hud.transform.Find("Commands")).rect.width,Is.EqualTo(300).Within(.01f));
                foreach(var card in cards)
                {
                    found.Add(card.name.Split(new[]{" · MP"},System.StringSplitOptions.None)[0]);
                    Assert.That(card.IsInteractable(),Is.True);Assert.That(((RectTransform)card.transform).rect.height,Is.EqualTo(28));
                }
                var next=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().SingleOrDefault(b=>b.name=="다음 기술 페이지");
                if(next==null||!next.IsInteractable())break;
                Click(next.name);yield return null;
            }
            Assert.That(found,Is.EquivalentTo(unit.Data.Skills.Concat(new[]{unit.Data.UltimateSkill}).Where(s=>s!=null&&director.Session.Resolver.CanUse(unit,s,director.IsFollowup(s))==null).Select(s=>s.DisplayName)));
            Click("기술 상세");yield return null;Assert.That(director.Hud.UnitDetailsOpen,Is.True);
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()).Select(b=>b.name),Is.EquivalentTo(new[]{"닫기"}));
            Click("닫기");yield return null;
            var last=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name.Contains(" · MP"));
            Click(last.name);yield return null;Assert.That(director.State,Is.TypeOf<TargetSelectionState>());
            director.State.Cancel();yield return null;Assert.That(director.State,Is.TypeOf<ActionSelectionState>());
            unit.CurrentMP=0;unit.Acted=true;director.Hud.Refresh();yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name.Contains(" · MP")),Is.False);
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="사용 가능한 기술 없음"),Is.True);
            director.GetComponent<GamepadPointer>().MoveTo(new Vector2(Screen.width-2,Screen.height/2));director.GetComponent<GamepadPointer>().Submit();yield return null;Assert.That(director.State,Is.TypeOf<CommandState>());
            unit.Acted=false;unit.CurrentMP=mp;Assert.That(unit.CurrentHP,Is.EqualTo(hp));
        }
        [UnityTest] public IEnumerator DirectSkillSelectionClearsOldTargetAndExecutesOnce()
        {
            director.BeginBattle();yield return null;var unit=director.Session.Active;
            var enemy=director.Session.Units.First(u=>u.Team==Team.Enemy);
            Assert.That(director.Session.Grid.Place(enemy,new Vector2Int(2,2)),Is.True);director.Board.Sync();
            var skill=unit.Data.Skills.First(s=>s.Target==TargetType.Enemy&&s.Gate==SkillGate.None&&s.MPCost>0&&!s.IsLionHowl&&s.MinRange<=1&&s.Range>=1);
            int mp=unit.CurrentMP,hp=enemy.CurrentHP;director.Target=enemy.Position;
            Click("Skill / 스킬");yield return null;Click(skill.DisplayName+" · MP"+skill.MPCost);yield return null;
            Assert.That(director.State,Is.TypeOf<TargetSelectionState>());Assert.That(director.Target,Is.Null);Assert.That(unit.CurrentMP,Is.EqualTo(mp));
            director.HandleBattleClick(TileScreen(enemy.Position));director.HandleBattleClick(TileScreen(enemy.Position));director.Confirm();
            float deadline=Time.realtimeSinceStartup+10;while(director.State is ActionExecutionState&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(unit.CurrentMP,Is.EqualTo(mp-skill.MPCost));Assert.That(enemy.CurrentHP,Is.LessThan(hp));Assert.That(unit.Acted,Is.True);
        }
        [UnityTest] public IEnumerator FullScreenSceneryKeepsTilesInHudSafeAreaAcrossRotations()
        {
            director.BeginBattle();yield return null;
            foreach(bool skills in new[]{false,true})
            {
                if(skills)director.SkillCommand();yield return null;
                for(int rotation=0;rotation<4;rotation++)
                {
                    director.Board.RotateCamera(90);yield return null;
                    var camera=director.Board.BattleCamera;Assert.That(camera.rect,Is.EqualTo(new Rect(0,0,1,1)));
                    var safe=director.Hud.BattlefieldViewport;
                    foreach(var tile in director.Session.Grid.Tiles.Values)
                    {
                        var p=camera.WorldToViewportPoint(tile.WorldPosition(director.Catalog.Rules.TileHeight));
                        Assert.That(safe.Contains(new Vector2(p.x,p.y)),Is.True,tile.Coordinate.ToString());
                    }
                }
            }
        }
    }
}
