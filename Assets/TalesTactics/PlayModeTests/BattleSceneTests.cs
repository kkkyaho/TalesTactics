using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        BattleDirector director;
        [UnitySetUp] public IEnumerator LoadBattle()
        {
            yield return SceneManager.LoadSceneAsync("TestBattle",LoadSceneMode.Single);
            yield return null;
            director=Object.FindAnyObjectByType<BattleDirector>();
            Assert.That(director,Is.Not.Null);
            Assert.That(director.enabled,Is.True,"Startup validation failed; inspect Console.");
            director.TrainingMode=true; // Never grant campaign EXP or write a save in these tests.
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var battle=SceneManager.GetSceneByName("TestBattle");
            var empty=SceneManager.CreateScene("Test cleanup");SceneManager.SetActiveScene(empty);
            if(battle.isLoaded)yield return SceneManager.UnloadSceneAsync(battle);
            LogAssert.NoUnexpectedReceived();
        }
        void Click(string name)
        {
            var button=Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name==name);
            Assert.That(button.interactable,Is.True);
            ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            if(name=="전투 시작"&&director.StoryActive)director.FinishStory();
        }
        [UnityTest] public IEnumerator CampaignVictoryUnlocksNextStageAndRetriesWithoutDuplicateRewards()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;director.SelectedStage=1;
            director.BeginBattle();Assert.That(director.Session,Is.Null);
            director.SelectedStage=0;int writes=0;director.PersistCampaign=_=>++writes>1;
            director.BeginBattle();yield return null;
            foreach(var u in director.Session.Units.Where(u=>u.Team==Team.Enemy))u.Damage(99999,director.Session.Grid);
            director.SetState(new TurnStartState(director));yield return null;
            Assert.That(director.RewardPending,Is.True);Assert.That(director.Campaign.StoryProgress,Is.Empty);
            Click("보상 저장 재시도");yield return null;
            Assert.That(director.RewardPending,Is.False);Assert.That(director.Campaign.StoryProgress,Does.Contain("chapter1"));
            director.CompleteBattle();director.SaveBattleReward();Assert.That(writes,Is.EqualTo(2));
            director.Restart();yield return null;director.SelectedStage=1;director.BeginBattle();yield return null;
            Assert.That(director.Session.Units.First(u=>u.Team==Team.Enemy).Level,Is.EqualTo(3));
            foreach(var u in director.Session.Units.Where(u=>u.Team==Team.Enemy))u.Damage(99999,director.Session.Grid);
            director.SetState(new TurnStartState(director));yield return null;
            Assert.That(director.Campaign.StoryProgress,Does.Contain("chapter2"));
        }
        [UnityTest] public IEnumerator DeploymentButtonStartsWiredScene()
        {
            Assert.That(director.Catalog.Characters.Length,Is.EqualTo(10));
            Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
            Click("전투 시작");yield return null;
            Assert.That(director.Session.Units.Count,Is.EqualTo(7));
            Assert.That(director.Session.Grid.Tiles.Count,Is.EqualTo(90));
            Assert.That(director.Board.BattleCamera.orthographic,Is.True);
            foreach(var tile in director.Session.Grid.Tiles.Values)
            {
                var screen=director.Board.BattleCamera.WorldToScreenPoint(tile.WorldPosition(director.Catalog.Rules.TileHeight));
                Assert.That(director.Board.BattleCamera.pixelRect.Contains(new Vector2(screen.x,screen.y)),Is.True,"Every tile must remain in the unobstructed battlefield viewport.");
            }
            Assert.That(director.State,Is.InstanceOf<CommandState>());
            Assert.That(director.Hud.Font.HasCharacter('크',true,true),Is.True);
            Assert.That(director.Hud.Font.HasCharacter('砕',true,true),Is.True,"Japanese fallback is required for Farah's canonical skill name.");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator MovementCoroutineAndUndoRestoreOccupancy()
        {
            Click("전투 시작");yield return null;
            var u=director.Session.Active;var origin=u.Position;
            var target=director.Session.Grid.Reachable(u,out _).Keys.First(p=>p!=origin);
            Click("Move / 이동");director.State.Tile(target);
            for(int i=0;i<240&&director.State is ActionExecutionState;i++)yield return null;
            Assert.That(director.State,Is.InstanceOf<CommandState>());
            Assert.That(u.Position,Is.EqualTo(target));Assert.That(u.Moved,Is.True);
            Click("Undo Move / 이동 취소");yield return null;
            Assert.That(u.Position,Is.EqualTo(origin));Assert.That(u.Moved,Is.False);
            Assert.That(director.Session.Grid[origin].Occupant,Is.SameAs(u));
        }
        [UnityTest] public IEnumerator CameraRotationZoomAndResetPreserveBattle()
        {
            Click("전투 시작");yield return null;
            var camera=director.Board.BattleCamera;
            var rotation=camera.transform.rotation;float size=camera.orthographicSize;
            var active=director.Session.Active;var position=active.Position;
            for(int i=0;i<4;i++)
            {
                Click("우회전");
                foreach(var tile in director.Session.Grid.Tiles.Values)
                {
                    var p=camera.WorldToScreenPoint(tile.WorldPosition(director.Catalog.Rules.TileHeight));
                    Assert.That(camera.pixelRect.Contains(new Vector2(p.x,p.y)),Is.True,"Rotation must fit the entire board.");
                }
            }
            Assert.That(Quaternion.Angle(rotation,camera.transform.rotation),Is.LessThan(0.01f));
            Click("확대 +");Assert.That(camera.orthographicSize,Is.LessThan(size));
            Assert.That(director.Board.Pick(new Vector2(-10,-10),out _),Is.False);
            Click("초기화");
            Assert.That(camera.orthographicSize,Is.EqualTo(size).Within(0.01f));
            Assert.That(active.Position,Is.EqualTo(position));Assert.That(active.Moved,Is.False);
            Assert.That(director.State,Is.InstanceOf<CommandState>());
            Click("Restart");yield return null;Click("전투 시작");yield return null;
            Assert.That(Quaternion.Angle(rotation,camera.transform.rotation),Is.LessThan(0.01f));
        }
        [UnityTest] public IEnumerator UnavailableSkillCanBeInspectedButCannotBeSelected()
        {
            Click("전투 시작");yield return null;
            var unit=director.Session.Active;int mp=unit.CurrentMP,hp=unit.CurrentHP;
            var skill=unit.Data.Skills.First(s=>s.MPCost>0&&s.Gate==SkillGate.None);
            unit.CurrentMP=0;Click("Skill / 스킬");yield return null;
            string button=Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name.StartsWith(skill.DisplayName+" · MP")).name;
            Click(button);yield return null;
            Assert.That(director.State,Is.InstanceOf<SkillDetailsState>());
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="목표 선택").interactable,Is.False);
            Assert.That(Object.FindObjectsByType<TMPro.TMP_Text>().Any(t=>t.text.Contains("MP가 부족")),Is.True);
            Assert.That(unit.CurrentMP,Is.Zero);Assert.That(unit.CurrentHP,Is.EqualTo(hp));Assert.That(unit.Acted,Is.False);
            Click("스킬 목록으로");yield return null;
            unit.CurrentMP=mp;director.Hud.Refresh();yield return null;
            button=Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name.StartsWith(skill.DisplayName+" · MP")).name;
            Click(button);yield return null;Click("목표 선택");yield return null;
            Assert.That(director.State,Is.InstanceOf<TargetSelectionState>());Assert.That(director.SelectedSkill,Is.SameAs(skill));
            Assert.That(unit.CurrentMP,Is.EqualTo(mp));Assert.That(unit.Acted,Is.False);
        }
        [UnityTest] public IEnumerator EquipmentDraftCancelAndTrainingBattleUseSavedLoadout()
        {
            director.Campaign=new CampaignSave(); // Isolate this test from the user's saved campaign.
            var character=director.Catalog.Characters.First(c=>c.Weapon==WeaponType.Sword);
            var sword=director.Catalog.Equipment.First(e=>e.Slot==EquipmentSlot.Weapon&&e.Weapon==character.Weapon);
            Click("장비 관리");yield return null;Click(character.DisplayName);yield return null;
            Click("무기: 없음");yield return null;
            Click("장착 후보: "+sword.DisplayName);yield return null;
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>().Any(b=>b.name=="무기: "+sword.DisplayName),Is.True);
            Assert.That(director.Campaign.Get(character.Id).Equipment[0],Is.Null);
            Click("돌아가기 (미적용 취소)");yield return null;Click(character.DisplayName);yield return null;
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>().Any(b=>b.name=="무기: 없음"),Is.True);
            Click("돌아가기 (미적용 취소)");yield return null;Click("출전 준비로");yield return null;
            director.Campaign.Get(character.Id).Equipment[0]=sword.Id;
            director.Deployment.Clear();director.Deployment.Add(System.Array.IndexOf(director.Catalog.Characters,character));
            Click("전투 시작");yield return null;
            var unit=director.Session.Units.Single(u=>u.Team==Team.Player);
            Assert.That(unit.Equipment[0],Is.SameAs(sword));Assert.That(unit.Stats.STR,Is.EqualTo(character.StatsAt(25).STR+sword.Bonus.STR));
            Assert.That(unit.CurrentHP,Is.EqualTo(unit.Stats.HP));
        }
        [UnityTest] public IEnumerator GrowthScreenShowsRequirementsAndPromotionAppliesToCampaign()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;
            var c=director.Catalog.Characters[0];var progress=director.Campaign.Get(c.Id);
            Click("성장 · 승급");yield return null;Click(c.DisplayName);yield return null;
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="승급 · 저장").interactable,Is.False);
            Assert.That(Object.FindObjectsByType<TMPro.TMP_Text>().Any(t=>t.text.Contains("미충족")),Is.True);
            Click("캐릭터 목록으로");yield return null;
            progress.Level=c.PromotionLevel;director.Campaign.StoryProgress.Add(c.PromotionStoryFlag);
            Click(c.DisplayName);yield return null;
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="승급 · 저장").interactable,Is.True);
            Assert.That(progress.Promoted,Is.False,"Preview must not promote.");
            // Exercise the same transaction with an isolated persistence callback, never the user's save.
            Assert.That(CampaignPromotion.TrySave(director.Campaign,c,_=>true),Is.True);
            Click("캐릭터 목록으로");yield return null;Click(c.DisplayName);yield return null;
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="승급 완료").interactable,Is.False);
            Click("캐릭터 목록으로");yield return null;Click("출전 준비로");yield return null;
            director.Deployment.Clear();director.Deployment.Add(0);Click("전투 시작");yield return null;
            var unit=director.Session.Units.Single(u=>u.Team==Team.Player);
            Assert.That(unit.Promoted,Is.True);Assert.That(unit.Stats.HP,Is.EqualTo(c.StatsAt(progress.Level,true).HP));
            Assert.That(unit.CurrentHP,Is.EqualTo(unit.Stats.HP));
        }        [UnityTest] public IEnumerator TargetPreviewExecutesAndTurnEnds()
        {
            Click("전투 시작");yield return null;
            var u=director.Session.Active;var enemy=director.Session.Units.First(x=>x.Team==Team.Enemy);
            var neighbor=director.Session.Grid.Neighbors(u.Position).First(t=>t.Walkable&&t.Occupant==null);
            Assert.That(director.Session.Grid.Place(enemy,neighbor.Coordinate),Is.True);director.RefreshViews();
            int hp=enemy.CurrentHP;Click("Attack / 공격");director.State.Tile(enemy.Position);
            Assert.That(director.Target.HasValue,Is.True);Assert.That(enemy.CurrentHP,Is.EqualTo(hp),"Preview must not deal damage.");
            Click("실행");for(int i=0;i<240&&director.State is ActionExecutionState;i++)yield return null;
            Assert.That(enemy.CurrentHP,Is.LessThan(hp));Assert.That(u.Acted,Is.True);
            Click("Wait / 방향 선택");Click("Back");yield return null;
            Assert.That(u.Facing,Is.EqualTo(Facing.Back));Assert.That(director.Session.Active,Is.Not.SameAs(u));
        }
        [UnityTest] public IEnumerator VictoryAndRestartRecoverKOUnits()
        {
            Click("전투 시작");yield return null;
            foreach(var u in director.Session.Units.Where(u=>u.Team==Team.Enemy))u.Damage(99999,director.Session.Grid);
            director.SetState(new TurnStartState(director));yield return null;
            Assert.That(director.State,Is.InstanceOf<BattleEndState>());
            Assert.That(director.Session.Result,Is.EqualTo(BattleResult.Victory));
            Click("출전 화면 / Restart");yield return null;
            Assert.That(director.Session,Is.Null);Click("전투 시작");yield return null;
            Assert.That(director.Session.Units.All(u=>u.Alive),Is.True);
        }
        [UnityTest] public IEnumerator DefeatShowsRestartCommand()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;director.PersistCampaign=_=>throw new System.Exception("Defeat must not save");
            Click("전투 시작");yield return null;
            foreach(var u in director.Session.Units.Where(u=>u.Team==Team.Player))u.Damage(99999,director.Session.Grid);
            director.SetState(new TurnStartState(director));yield return null;
            Assert.That(director.Session.Result,Is.EqualTo(BattleResult.Defeat));
            Assert.That(director.Campaign.StoryProgress,Is.Empty);
            Assert.That(director.State,Is.InstanceOf<BattleEndState>());Click("출전 화면 / Restart");yield return null;
            Assert.That(director.Session,Is.Null);
        }
    }
}
