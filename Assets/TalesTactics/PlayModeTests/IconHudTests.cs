using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator IconCardsExposeEverySkillAndKeepSelectionCostsUnchanged()
        {
            director.BeginBattle();yield return null;
            var unit=director.Session.Active;int hp=unit.CurrentHP,mp=unit.CurrentMP;
            Click("Skill / 스킬");yield return null;
            var found=new System.Collections.Generic.HashSet<string>();
            for(int page=0;page<4;page++)
            {
                var cards=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.name.Contains(" · MP")).ToArray();
                Assert.That(cards.Length,Is.InRange(1,4));
                foreach(var card in cards)
                {
                    found.Add(card.name.Split(new[]{" · MP"},System.StringSplitOptions.None)[0]);
                    var icon=card.GetComponentInChildren<TacticalIcon>();Assert.That(icon,Is.Not.Null);Assert.That(icon.canvasRenderer,Is.Not.Null);
                    Assert.That(icon.raycastTarget,Is.False);
                }
                var next=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="다음 기술 페이지");
                if(!next.IsInteractable())break;
                Click(next.name);yield return null;
            }
            Assert.That(found,Is.EquivalentTo(unit.Data.Skills.Concat(new[]{unit.Data.UltimateSkill}).Where(s=>s!=null).Select(s=>s.DisplayName)));
            var last=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name.Contains(" · MP"));
            Click(last.name);yield return null;var selected=((SkillDetailsState)director.State).Skill;
            Click("기술 상세");yield return null;Assert.That(director.Hud.UnitDetailsOpen,Is.True);
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()).Select(b=>b.name),Is.EquivalentTo(new[]{"닫기"}));
            Click("닫기");yield return null;Assert.That(((SkillDetailsState)director.State).Skill,Is.SameAs(selected));
            Click("이전 기술 페이지");yield return null;Assert.That(director.State,Is.TypeOf<ActionSelectionState>());
            Assert.That(unit.CurrentHP,Is.EqualTo(hp));Assert.That(unit.CurrentMP,Is.EqualTo(mp));Assert.That(unit.Acted,Is.False);
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
