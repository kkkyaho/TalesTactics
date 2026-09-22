using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public class BattleRuleTests
    {
        BattleRules rules;CharacterData data;GridMap grid;UnitRuntime player,enemy;SkillResolver resolver;
        readonly List<ScriptableObject> created=new List<ScriptableObject>();
        T New<T>() where T:ScriptableObject {var o=ScriptableObject.CreateInstance<T>();created.Add(o);return o;}
        [SetUp] public void Setup()
        {
            rules=New<BattleRules>();data=New<CharacterData>();data.Id="tester";data.DisplayName="Tester";data.BaseStats=new Stats{HP=200,MP=120,STR=50,MAG=45,DEF=20,MDF=15,SPD=20,MOV=4,JMP=1};
            grid=GridMap.TestStage();player=new UnitRuntime(data,Team.Player,rules,25);enemy=new UnitRuntime(data,Team.Enemy,rules,25);grid.Place(player,new Vector2Int(1,1));grid.Place(enemy,new Vector2Int(2,1));
            resolver=new SkillResolver(grid,new[]{player,enemy},rules);
        }
        [TearDown] public void Cleanup(){foreach(var o in created)Object.DestroyImmediate(o);created.Clear();}
        SkillData Skill(EffectKind kind,TargetType target=TargetType.Enemy)
        {var s=New<SkillData>();s.Id="test";s.DisplayName="Test";s.Range=6;s.MinRange=0;s.Target=target;s.Effects=new[]{new SkillEffect{Kind=kind}};return s;}
        [Test] public void OccupiedTileCannotBeTraversed(){var reachable=grid.Reachable(player,out _);Assert.That(reachable.ContainsKey(enemy.Position),Is.False);}
        [Test] public void WaterCostsTwoMovement(){grid.Place(player,new Vector2Int(3,0));var costs=grid.Reachable(player,out _);Assert.That(costs[new Vector2Int(4,0)],Is.EqualTo(2));}
        [Test] public void JumpLimitBlocksCliff(){grid[new Vector2Int(1,2)].Height=4;Assert.That(grid.Reachable(player,out _).ContainsKey(new Vector2Int(1,2)),Is.False);}
        [Test] public void PathStartsAndEndsCorrectly(){var path=grid.Path(player,new Vector2Int(1,3));Assert.That(path.First(),Is.EqualTo(player.Position));Assert.That(path.Last(),Is.EqualTo(new Vector2Int(1,3)));}
        [Test] public void RootPreventsMovement(){player.AddStatus(StatusKind.Root,1);Assert.That(grid.Reachable(player,out _).Count,Is.EqualTo(1));}
        [Test] public void SchedulerSkipsKO(){enemy.CurrentHP=0;var scheduler=new SpeedTurnScheduler();Assert.That(scheduler.Next(new[]{player,enemy}),Is.SameAs(player));Assert.That(scheduler.Next(new[]{player,enemy}),Is.SameAs(player));}
        [Test] public void OneActionPerTurn(){var s=Skill(EffectKind.Damage);Assert.That(resolver.Execute(player,s,enemy.Position,out _),Is.True);Assert.That(resolver.Execute(player,s,enemy.Position,out _),Is.False);}
        [Test] public void InvalidTargetDoesNotSpendMP(){var s=Skill(EffectKind.Damage);s.MPCost=25;int mp=player.CurrentMP;Assert.That(resolver.Execute(player,s,player.Position,out _),Is.False);Assert.That(player.CurrentMP,Is.EqualTo(mp));}
        [Test] public void DamageDoesNotMutateCharacterData(){var hp=data.BaseStats.HP;enemy.Damage(50,grid);Assert.That(data.BaseStats.HP,Is.EqualTo(hp));}
        [Test] public void KOClearsOccupancy(){enemy.Damage(999,grid);Assert.That(enemy.Alive,Is.False);Assert.That(grid[enemy.Position].Occupant,Is.Null);}
        [Test] public void AutoReviveIsConsumedOnce(){enemy.AddStatus(StatusKind.AutoRevive,5);enemy.Damage(999,grid);Assert.That(enemy.Alive,Is.True);Assert.That(enemy.Has(StatusKind.AutoRevive),Is.False);enemy.Damage(999,grid);Assert.That(enemy.Alive,Is.False);}
        [Test] public void ReviveRestoresOccupancy(){enemy.Damage(999,grid);var ally=new UnitRuntime(data,Team.Enemy,rules);grid.Place(ally,new Vector2Int(3,1));var revive=Skill(EffectKind.Revive,TargetType.FallenAlly);var r=new SkillResolver(grid,new[]{ally,enemy},rules);Assert.That(r.Execute(ally,revive,enemy.Position,out _),Is.True);Assert.That(grid[enemy.Position].Occupant,Is.SameAs(enemy));}
        [Test] public void ReviveCannotOverlapLivingUnit(){enemy.Damage(999,grid);grid.Place(player,enemy.Position);var ally=new UnitRuntime(data,Team.Enemy,rules);grid.Place(ally,new Vector2Int(3,1));var r=new SkillResolver(grid,new[]{ally,enemy,player},rules);Assert.That(r.Execute(ally,Skill(EffectKind.Revive,TargetType.FallenAlly),enemy.Position,out _),Is.False);}
        [Test] public void RearAttackOutdamagesFront(){var e=new SkillEffect{Kind=EffectKind.Damage};enemy.Facing=Facing.Left;int front=resolver.DamagePreview(player,enemy,e);enemy.Facing=Facing.Right;Assert.That(resolver.DamagePreview(player,enemy,e),Is.GreaterThan(front));}
        [Test] public void GuardProtectsFrontNotRear(){var effect=new SkillEffect{Kind=EffectKind.Damage};enemy.Facing=Facing.Left;int baseDamage=resolver.DamagePreview(player,enemy,effect);enemy.AddStatus(StatusKind.Guard,2);Assert.That(resolver.DamagePreview(player,enemy,effect),Is.LessThan(baseDamage));enemy.Facing=Facing.Right;int rear=resolver.DamagePreview(player,enemy,effect);enemy.Statuses.Clear();Assert.That(resolver.DamagePreview(player,enemy,effect),Is.EqualTo(rear));}
        [Test] public void ConsumeClawLastsThreeSubsequentOwnTurns(){var s=Skill(EffectKind.ConsumeClaw,TargetType.Self);Assert.That(resolver.Execute(player,s,player.Position,out _),Is.True);player.EndTurn();for(int i=0;i<3;i++){Assert.That(player.Has(StatusKind.ConsumeClaw),Is.True);player.BeginTurn();player.EndTurn();}Assert.That(player.Has(StatusKind.ConsumeClaw),Is.False);}
        [Test] public void NightmareRequiresPriorAttack(){var s=Skill(EffectKind.Damage);s.Gate=SkillGate.Nightmare;player.AddStatus(StatusKind.ConsumeClaw,3);Assert.That(resolver.CanUse(player,s),Is.Not.Null);player.ClawAttacks=1;Assert.That(resolver.CanUse(player,s),Is.Null);}
        [Test] public void HPCostCannotKOUser(){var s=Skill(EffectKind.Damage);s.HPCost=player.CurrentHP;Assert.That(resolver.CanUse(player,s),Is.Not.Null);}
        [Test] public void FlamingEdgeNeedsChain(){var s=Skill(EffectKind.Damage);s.Gate=SkillGate.FlamingEdge;Assert.That(resolver.CanUse(player,s,true),Is.Not.Null);player.FlamingChain=true;player.Acted=true;Assert.That(resolver.CanUse(player,s,true),Is.Null);}
        [Test] public void StunOneTurnExpiresAfterOneTurn(){player.AddStatus(StatusKind.Stun,1);player.BeginTurn();Assert.That(player.Has(StatusKind.Stun),Is.True);player.EndTurn();Assert.That(player.Has(StatusKind.Stun),Is.False);}
        [Test] public void HealClampsToMaximum(){var s=Skill(EffectKind.Heal,TargetType.Self);s.Effects[0].Power=100;resolver.Execute(player,s,player.Position,out _);Assert.That(player.CurrentHP,Is.EqualTo(player.Stats.HP));}
        [Test] public void PullRespectsOccupancy(){grid.Place(enemy,new Vector2Int(5,1));var s=Skill(EffectKind.Pull);s.Area=3;s.Effects[0].Distance=3;resolver.Execute(player,s,new Vector2Int(2,1),out _);Assert.That(enemy.Position,Is.EqualTo(new Vector2Int(2,1)));Assert.That(grid[enemy.Position].Occupant,Is.SameAs(enemy));}
        [Test] public void CooldownBlocksReuseUntilNextTurn(){var s=Skill(EffectKind.Heal,TargetType.Self);s.Cooldown=1;resolver.Execute(player,s,player.Position,out _);player.BeginTurn();Assert.That(resolver.CanUse(player,s),Is.Not.Null);player.BeginTurn();Assert.That(resolver.CanUse(player,s),Is.Null);}
        [Test] public void VictoryAndDefeatAreDetected(){var c=new EliminateEnemies();Assert.That(c.Evaluate(new[]{player,enemy}),Is.EqualTo(BattleResult.Ongoing));enemy.Damage(999,grid);Assert.That(c.Evaluate(new[]{player,enemy}),Is.EqualTo(BattleResult.Victory));player.Damage(999,grid);Assert.That(c.Evaluate(new[]{player,enemy}),Is.EqualTo(BattleResult.Defeat));}
        [Test] public void FixedGrowthAndLevelCap(){data.GrowthStats=new Stats{STR=2};Assert.That(data.StatsAt(3).STR,Is.EqualTo(54));Assert.That(data.StatsAt(99).STR,Is.EqualTo(data.StatsAt(50).STR));}
        [Test] public void PromotionRequiresStoryAndLevel(){var progress=new CharacterProgress{Level=20};Assert.That(progress.TryPromote(data,new List<string>()),Is.False);Assert.That(progress.TryPromote(data,new List<string>{data.PromotionStoryFlag}),Is.True);Assert.That(progress.TryPromote(data,new List<string>{data.PromotionStoryFlag}),Is.False);}
        BattleSession Session()
        {
            var catalog=New<BattleCatalog>();catalog.Rules=rules;catalog.Characters=new[]{data};catalog.Enemy=data;data.BasicAttack=Skill(EffectKind.Damage);data.BasicAttack.Range=1;
            return new BattleSession(catalog,new[]{0});
        }
        [Test] public void MoveCanBeUndoneAndCannotRepeat(){var b=Session();b.Advance();var origin=b.Active.Position;Assert.That(b.Move(origin+Vector2Int.up),Is.True);Assert.That(b.Move(origin+Vector2Int.up),Is.False);Assert.That(b.UndoMove(),Is.True);Assert.That(b.Active.Position,Is.EqualTo(origin));Assert.That(b.Active.Moved,Is.False);}
        [Test] public void AttackThenMoveAllowedButAttackCommitsPriorMove(){var b=Session();b.Advance();var u=b.Active;var target=b.Units.First(x=>x.Team==Team.Enemy);b.Grid.Place(target,u.Position+Vector2Int.right);Assert.That(b.Resolver.Execute(u,u.Data.BasicAttack,target.Position,out _),Is.True);Assert.That(b.Move(u.Position+Vector2Int.up),Is.True);Assert.That(b.UndoMove(),Is.True);}
        [Test] public void ActionAfterMoveDisablesUndo(){var b=Session();b.Advance();var u=b.Active;b.Move(u.Position+Vector2Int.up);var target=b.Units.First(x=>x.Team==Team.Enemy);b.Grid.Place(target,u.Position+Vector2Int.right);b.Resolver.Execute(u,u.Data.BasicAttack,target.Position,out _);Assert.That(b.UndoMove(),Is.False);}
        [Test] public void AutomatedBattleReachesAnOutcome()
        {
            var b=Session();var planner=new EnemyPlanner();int turns=0;
            while(b.Result==BattleResult.Ongoing&&turns++<300)
            {
                b.Advance();var u=b.Active;var plan=planner.Plan(b,u);
                if(plan.Destination!=u.Position)b.Move(plan.Destination);
                if(plan.Target!=null)b.Resolver.Execute(u,plan.Skill,plan.Target.Position,out _);
                u.EndTurn();
                foreach(var tile in b.Grid.Tiles.Values)if(tile.Occupant!=null){Assert.That(tile.Occupant.Alive,Is.True);Assert.That(tile.Occupant.Position,Is.EqualTo(tile.Coordinate));}
            }
            Assert.That(b.Result,Is.Not.EqualTo(BattleResult.Ongoing));
        }
        BattleCatalog ValidCatalog()
        {var c=New<BattleCatalog>();c.Rules=rules;c.Characters=new[]{data};c.Enemy=data;data.BasicAttack=Skill(EffectKind.Damage);return c;}
        [Test] public void CatalogReportsMissingRosterReference()
        {var c=ValidCatalog();c.Characters=new CharacterData[]{null};Assert.That(CatalogValidation.TryValidate(c,out var error),Is.False);Assert.That(error,Does.Contain("Characters[0]"));}
        [Test] public void CatalogRejectsMissingAttack()
        {var c=ValidCatalog();data.BasicAttack=null;Assert.That(CatalogValidation.TryValidate(c,out var error),Is.False);Assert.That(error,Does.Contain("BasicAttack"));}
        [Test] public void CatalogRejectsDuplicateRosterIDs()
        {var c=ValidCatalog();c.Characters=new[]{data,data};Assert.That(CatalogValidation.TryValidate(c,out var error),Is.False);Assert.That(error,Does.Contain("Duplicate"));}
        [Test] public void ValidCatalogCanStartBattle()
        {Assert.That(CatalogValidation.TryValidate(ValidCatalog(),out var error),Is.True,error);}
        EquipmentData Sword()
        {var item=New<EquipmentData>();item.Id="sword";item.Slot=EquipmentSlot.Weapon;item.Weapon=WeaponType.Sword;item.Bonus=new Stats{STR=4};return item;}
        string SaveTestPath()
        {
            string folder=System.IO.Path.Combine(Application.persistentDataPath,"StorageTests",System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(folder);return System.IO.Path.Combine(folder,"campaign.json");
        }
        [Test] public void SaveNormalizationRepairsLegacyFieldsWithoutLosingGear()
        {
            var s=new CampaignSave{StoryProgress=null};s.Characters.Add(new CharacterProgress{Id="hero",Level=0,EXP=-1,Equipment=new[]{"sword"},UnlockedSkills=null});
            CampaignFile.Normalize(s);var p=s.Get("hero");Assert.That(p.Level,Is.EqualTo(1));Assert.That(p.EXP,Is.EqualTo(0));
            Assert.That(p.Equipment.Length,Is.EqualTo(3));Assert.That(p.Equipment[0],Is.EqualTo("sword"));Assert.That(p.UnlockedSkills,Is.Not.Null);Assert.That(s.StoryProgress,Is.Not.Null);
        }
        [Test] public void CampaignFileRoundTripPreservesProgressAndBackup()
        {
            string path=SaveTestPath();var store=new CampaignFile(path);var s=store.Load();var p=s.Get("hero");p.Level=12;p.Promoted=true;p.Equipment[0]="sword";s.StoryProgress.Add("chapter2");
            Assert.That(store.Save(s),Is.True);p.Level=13;Assert.That(store.Save(s),Is.True);
            var loaded=new CampaignFile(path).Load();Assert.That(loaded.Get("hero").Level,Is.EqualTo(13));Assert.That(loaded.Get("hero").Promoted,Is.True);Assert.That(loaded.Get("hero").Equipment[0],Is.EqualTo("sword"));Assert.That(loaded.StoryProgress,Does.Contain("chapter2"));
            Assert.That(new CampaignFile(path+".bak").Load().Get("hero").Level,Is.EqualTo(12));
        }
        [Test] public void CorruptPrimaryRecoversBackupAndArchivesOriginalOnSave()
        {
            string path=SaveTestPath();var s=new CampaignSave();s.Get("hero").Level=7;
            string backup=JsonUtility.ToJson(s);System.IO.File.WriteAllText(path+".bak",backup);System.IO.File.WriteAllText(path,"broken");
            var store=new CampaignFile(path);var loaded=store.Load();Assert.That(loaded.Get("hero").Level,Is.EqualTo(7));Assert.That(store.Notice,Does.Contain("백업"));
            Assert.That(store.Save(loaded),Is.True);Assert.That(System.IO.File.ReadAllText(path+".bak"),Is.EqualTo(backup));
            var archive=System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(path),"*.corrupt-*");Assert.That(archive.Length,Is.EqualTo(1));Assert.That(System.IO.File.ReadAllText(archive[0]),Is.EqualTo("broken"));
        }
        [Test] public void FutureVersionIsNeverReplacedByOlderBackupOrSave()
        {
            string path=SaveTestPath();string future="{\"Version\":99,\"Characters\":[]}";System.IO.File.WriteAllText(path,future);System.IO.File.WriteAllText(path+".bak",JsonUtility.ToJson(new CampaignSave()));
            var store=new CampaignFile(path);var s=store.Load();Assert.That(store.CanSave,Is.False);Assert.That(store.Save(s),Is.False);Assert.That(System.IO.File.ReadAllText(path),Is.EqualTo(future));
        }
        [Test] public void UnrecoverableSaveBlocksAutomaticOverwrite()
        {
            string path=SaveTestPath();System.IO.File.WriteAllText(path,"{}");var store=new CampaignFile(path);var s=store.Load();
            Assert.That(store.Save(s),Is.False);Assert.That(System.IO.File.ReadAllText(path),Is.EqualTo("{}"));Assert.That(System.IO.File.Exists(path+".tmp"),Is.False);
        }
        [Test] public void DuplicateCharacterIDsRecoverFromValidBackup()
        {
            string path=SaveTestPath();var s=new CampaignSave();s.Characters.Add(new CharacterProgress{Id="hero"});s.Characters.Add(new CharacterProgress{Id="hero"});
            System.IO.File.WriteAllText(path,JsonUtility.ToJson(s));var backup=new CampaignSave();backup.Get("hero").Level=8;System.IO.File.WriteAllText(path+".bak",JsonUtility.ToJson(backup));
            Assert.That(new CampaignFile(path).Load().Get("hero").Level,Is.EqualTo(8));
        }
        [Test] public void EquipmentDraftRejectsWrongWeaponAndSlot()
        {
            var sword=Sword();data.Weapon=WeaponType.Staff;var draft=new EquipmentLoadout(data,new CharacterProgress(),new[]{sword});
            Assert.That(draft.Equip(EquipmentSlot.Weapon,sword),Is.False);
            data.Weapon=WeaponType.Sword;Assert.That(draft.Equip(EquipmentSlot.Armor,sword),Is.False);
            Assert.That(draft.Equip(EquipmentSlot.Weapon,sword),Is.True);
            Assert.That(draft.Preview(1,false).STR,Is.EqualTo(data.BaseStats.STR+4));
        }
        [Test] public void EquipmentDraftOnlyCommitsAfterSuccessfulSave()
        {
            var sword=Sword();data.Weapon=WeaponType.Sword;var campaign=new CampaignSave();var progress=campaign.Get(data.Id);
            var draft=new EquipmentLoadout(data,progress,new[]{sword});draft.Equip(EquipmentSlot.Weapon,sword);
            Assert.That(progress.Equipment[0],Is.Null);var original=progress.Equipment;
            Assert.That(draft.TrySave(campaign,_=>false),Is.False);Assert.That(progress.Equipment,Is.SameAs(original));
            Assert.That(draft.TrySave(campaign,s=>s.Get(data.Id).Equipment[0]==sword.Id),Is.True);
            Assert.That(progress.Equipment[0],Is.EqualTo(sword.Id));draft.Cycle(EquipmentSlot.Weapon);
            Assert.That(progress.Equipment[0],Is.EqualTo(sword.Id),"Draft changes after saving must remain detached.");
        }
        [Test] public void MissingOrLegacyEquipmentIDsLoadAsEmptySlots()
        {
            var sword=Sword();var progress=new CharacterProgress{Equipment=new[]{"missing"}};
            var draft=new EquipmentLoadout(data,progress,new[]{sword});Assert.That(draft.ExportIDs().Length,Is.EqualTo(3));
            Assert.That(draft.ExportIDs().All(id=>id==null),Is.True);
            progress.Equipment=null;Assert.That(new EquipmentLoadout(data,progress,null).ExportIDs().All(id=>id==null),Is.True);
        }
        [Test] public void SkillDetailsUseActualHPAndResourceCosts()
        {
            var skill=Skill(EffectKind.Damage);skill.HPPercentCost=0.15f;skill.HPCost=3;skill.MPCost=12;skill.GaugeCost=25;
            string detail=resolver.Describe(player,skill);
            Assert.That(detail,Does.Contain("HP "+resolver.HPCost(player,skill)));
            Assert.That(detail,Does.Contain("MP 12"));Assert.That(detail,Does.Contain("게이지 25"));
            Assert.That(player.Acted,Is.False);
        }
        [Test] public void MixedEffectsPreviewNamesCasterAndDoesNotMutateResources()
        {
            var skill=Skill(EffectKind.Damage);skill.Effects=new[]{new SkillEffect{Kind=EffectKind.Damage},new SkillEffect{Kind=EffectKind.Heal,AffectCaster=true},new SkillEffect{Kind=EffectKind.Pull,Distance=2}};
            int hp=player.CurrentHP,mp=player.CurrentMP,enemyHP=enemy.CurrentHP;
            string preview=resolver.Preview(player,skill,enemy);
            Assert.That(preview,Does.Contain("시전자: 회복량"));Assert.That(preview,Does.Contain("끌어당김 최대 2칸"));
            Assert.That(player.CurrentHP,Is.EqualTo(hp));Assert.That(player.CurrentMP,Is.EqualTo(mp));Assert.That(enemy.CurrentHP,Is.EqualTo(enemyHP));
        }
    }
}
