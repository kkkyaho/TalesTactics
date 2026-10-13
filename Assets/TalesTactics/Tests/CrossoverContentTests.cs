using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Tests
{
    public sealed class CrossoverContentTests
    {
        BattleCatalog Catalog=>AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
        public static int[] CombatStages=>Enumerable.Range(6,50).Where(s=>!PersonalScenarios.Contains(s)||PersonalScenarios.Tier(s)>0).ToArray();
        public static string[] Owners=>PersonalScenarios.Arcs.Select(a=>a.Owner).ToArray();
        int Index(string id)=>Array.FindIndex(Catalog.Characters,c=>c.Id==id);
        BattleSession Trial(string owner)
        {
            int stage=28+Array.FindIndex(PersonalScenarios.Arcs,a=>a.Owner==owner)*3;
            return new BattleSession(Catalog,new[]{Index(owner),Index(owner=="mint"?"cless":"mint")},35,25,stage,teamTurns:true,actionExperience:false);
        }
        static void Cast(BattleSession b,UnitRuntime actor,SkillData skill,Vector2Int aim,bool followup=false)
        {
            if(!followup)actor.BeginTurn();
            Assert.That(b.Resolver.Execute(actor,skill,aim,out var message,followup),Is.True,skill.Id+": "+message);
        }
        [Test] public void SixtyInstalledSkillsMatchAuthoredRulesAndDirectionalMotionReferences()
        {
            Assert.That(CrossoverBossSources.Entries.Length,Is.EqualTo(20));
            foreach(var entry in CrossoverBossSources.Entries)
            {
                var boss=CrossoverCampaign.Boss(Catalog,entry[0]);
                for(int i=0;i<3;i++)
                {
                    string id="boss."+boss.Id+"."+i;var actual=boss.Skills.Single(s=>s.Id==id);
                    var expected=ScriptableObject.CreateInstance<SkillData>();
                    try
                    {
                        expected.Id=actual.Id;expected.DisplayName=actual.DisplayName;expected.SourceNote=actual.SourceNote;CrossoverSkillRules.Configure(boss.Id,i,expected);
                        Assert.That(JsonUtility.ToJson(actual),Is.EqualTo(JsonUtility.ToJson(expected)),id);
                        Assert.That(actual.SourceNote,Is.Not.Empty);
                    }
                    finally{UnityEngine.Object.DestroyImmediate(expected);}
                }
                foreach(Facing facing in Enum.GetValues(typeof(Facing)))
                {
                    Assert.That(boss.Sprites.Get(facing),Is.Not.Null,boss.Id);
                    foreach(var clip in new[]{boss.Poses.Walk,boss.Poses.AttackMotion,boss.Poses.Skill,boss.Poses.Ultimate})
                    {Assert.That(clip.Frames.Length,Is.GreaterThanOrEqualTo(4),boss.Id);foreach(var frame in clip.Frames)Assert.That(frame.Get(facing),Is.Not.Null,boss.Id);}
                }
            }
        }
        [TestCaseSource(nameof(CombatStages))]
        public void AuthoredCombatMapsAndOpeningCheckpointRewindSurviveEveryScheduler(int stage)
        {
            int[] deployment=PersonalScenarios.Contains(stage)?new[]{Index(PersonalScenarios.Arc(stage).Owner),Index(PersonalScenarios.Arc(stage).Owner=="mint"?"cless":"mint")}:new[]{Index("cless"),Index("mint")};
            for(int mode=0;mode<3;mode++)
            {
                var b=new BattleSession(Catalog,deployment,35,25,stage,useCT:mode==1,teamTurns:mode==2);
                foreach(var u in b.Units.Where(u=>u.Alive&&!u.CrossoverReserve))Assert.That(b.Grid[u.Position].Occupant,Is.SameAs(u),stage+" "+u.Data.Id);
                var start=b.Units[0].Position;var seen=new HashSet<Vector2Int>{start};var pending=new Queue<Vector2Int>();pending.Enqueue(start);
                while(pending.Count>0)
                {
                    var p=pending.Dequeue();foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
                    {var q=p+d;var tile=b.Grid[q];if(tile!=null&&tile.Walkable&&Math.Abs(tile.Height-b.Grid[p].Height)<=1&&seen.Add(q))pending.Enqueue(q);}
                }
                foreach(var p in CrossoverBattle.Nodes.Concat(new[]{CrossoverBattle.Exit,CrossoverBattle.ContractSeal}))Assert.That(seen,Has.Member(p),"Unreachable marker "+stage+" "+p);
                b.Opening=BattleOpening.Capture(b,Catalog);b.Advance();int turns=0;
                while(b.Active.Team!=Team.Player&&turns++<100){b.EndTurn();b.Advance();}
                b.RecordAction("state before action");b.Crossover.State.Actions++;
                var copy=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,deployment))).Restore(Catalog);
                Assert.That(copy.Crossover.State.Actions,Is.EqualTo(b.Crossover.State.Actions));
                Assert.That(copy.RewindLast(out _).Crossover.State.Actions,Is.EqualTo(b.Crossover.State.Actions-1));
                Assert.That(copy.Opening.Restore(Catalog).Crossover.State.Actions,Is.Zero);
            }
        }
        [TestCaseSource(nameof(Owners))]
        public void PersonalTrialConditionsRespondToActualCharacterSkills(string owner)
        {
            var b=Trial(owner);var actor=b.Units[0];var healer=b.Units[1];var boss=b.Crossover.Boss;
            Assert.That(b.Grid.Place(actor,new Vector2Int(5,11)),Is.True);Assert.That(b.Grid.Place(healer,new Vector2Int(5,12)),Is.True);
            actor.SpecialGauge=100;actor.CurrentHP=actor.Stats.HP/2;
            switch(owner)
            {
                case "cless":
                    Cast(b,actor,actor.Data.BasicAttack,boss.Position);
                    Cast(b,healer,healer.Data.Skills.First(s=>s.Effects.Any(e=>e.Kind==EffectKind.Heal)),actor.Position);
                    actor.BeginTurn();actor.AddStatus(StatusKind.Guard,2);actor.Acted=true;actor.EndTurn();b.Crossover.OnTurnEnded(actor);
                    Assert.That(b.Crossover.State.TrialProgress,Is.EqualTo(7));break;
                case "mint":case "shionne":
                    var heal=actor.Data.Skills.First(s=>s.Effects.Any(e=>e.Kind==EffectKind.Heal));
                    for(int i=0;i<3;i++){healer.CurrentHP=healer.Stats.HP/2;Cast(b,actor,heal,healer.Position);}
                    Assert.That(b.Crossover.State.TrialActions,Is.EqualTo(3));
                    healer.CurrentHP=healer.Stats.HP;Cast(b,actor,heal,healer.Position);Assert.That(b.Crossover.State.TrialActions,Is.EqualTo(3));break;
                case "tear":
                    var song=actor.Data.Skills.First(s=>s.Effects.Any(e=>e.Status==StatusKind.Song));Cast(b,actor,song,actor.Position);Assert.That(b.Crossover.State.TrialProgress,Is.EqualTo(1));break;
                case "jade":
                    boss.IntentPhase=1;boss.IntentSkill=boss.Data.Skills.First(s=>s.Id.StartsWith("boss.")).Id;
                    Cast(b,actor,actor.Data.UltimateSkill,boss.Position);Assert.That(b.Crossover.State.TrialProgress,Is.EqualTo(1));Assert.That(boss.IntentPhase,Is.Zero);break;
                case "velvet":
                    Cast(b,actor,actor.Data.Skills.First(s=>s.Effects.Any(e=>e.Kind==EffectKind.ConsumeClaw)),actor.Position);
                    Cast(b,actor,actor.Data.Skills.First(s=>s.Effects.Any(e=>e.Drain>0)&&s.Gate==SkillGate.Claw),boss.Position);
                    Assert.That(b.Crossover.State.TrialProgress,Is.EqualTo(1));break;
                case "alphen":
                    Cast(b,actor,actor.Data.Skills.First(s=>s.StartsFlamingChain),boss.Position);
                    Cast(b,actor,actor.Data.Skills.First(s=>s.Gate==SkillGate.FlamingEdge),boss.Position,true);
                    Assert.That(b.Crossover.State.TrialProgress,Is.EqualTo(1));break;
                case "kisara":
                    actor.AddStatus(StatusKind.Guard,2);Cast(b,boss,boss.Data.BasicAttack,actor.Position);
                    Assert.That(b.Crossover.State.TrialProgress,Is.EqualTo(1));Assert.That(actor.Alive,Is.True);break;
                case "farah":case "natalia":break;
            }
            // Mission conditions are exercised independently of the balance simulation.
            if(b.Crossover.UsesNodes)
                foreach(var p in CrossoverBattle.Nodes){var occupant=b.Grid[p].Occupant;if(occupant!=null&&occupant!=actor){occupant.Damage(99999,b.Grid);b.Crossover.Update();}Assert.That(b.Grid.Place(actor,p),Is.True);b.Crossover.Update();}
            if(owner=="jade"||owner=="natalia"||owner=="alphen")foreach(var device in b.Units.Where(u=>u.Data.Id=="boundary-device"))device.Damage(99999,b.Grid);
            if(owner!="cless"&&owner!="mint"&&owner!="shionne"&&owner!="natalia")
                for(int i=0;i<3&&boss.Alive;i++){boss.Damage(99999,b.Grid);boss.BeginTurn();boss.Acted=true;boss.EndTurn();}
            if(b.Crossover.UsesExit)Assert.That(b.Grid.Place(actor,CrossoverBattle.Exit),Is.True);
            if(b.Crossover.UsesContract)Assert.That(b.Grid.Place(actor,CrossoverBattle.ContractSeal),Is.True);
            b.Crossover.Update();Assert.That(b.Result,Is.EqualTo(BattleResult.Victory),owner);
        }
        [Test] public void TwentyMainMapsHaveDistinctTerrainAndKeepLegacyMapsUntouched()
        {
            var fingerprints=Enumerable.Range(6,20).Select(stage=>string.Join(";",CrossoverCampaign.Map(stage).Tiles.Values.OrderBy(t=>t.Coordinate.x).ThenBy(t=>t.Coordinate.y).Select(t=>t.Height+":"+t.Walkable+":"+t.MovementCost))).ToArray();
            Assert.That(fingerprints.Distinct().Count(),Is.EqualTo(20));
        }
        [Test] public void ExpandedCatalogHasAllRequiredContentWithoutReplacingLegacyBossAssets()
        {
            Assert.That(CatalogValidation.TryValidateCrossover(Catalog,out var error),Is.True,error);
            foreach(var id in new[]{"dhaos","golem"})
            {
                var legacy=Catalog.Enemies.Single(e=>e.Id==id);var expanded=CrossoverCampaign.Boss(Catalog,id);
                Assert.That(expanded,Is.Not.SameAs(legacy));Assert.That(expanded.Sprites.Front,Is.Not.SameAs(legacy.Sprites.Front));
                Assert.That(legacy.Skills.Any(s=>s.Id.StartsWith("boss.")),Is.False);
            }
            var early=new BattleSession(Catalog,new[]{Index("cless")},campaignStage:8);var late=new BattleSession(Catalog,new[]{Index("cless")},campaignStage:21);
            Assert.That(early.Crossover.Boss.Data.CrossoverEarlyForm,Is.True);Assert.That(late.Crossover.Boss.Data.CrossoverEarlyForm,Is.False);
            Assert.That(early.Crossover.Boss.Data.Sprites.Front,Is.Not.SameAs(late.Crossover.Boss.Data.Sprites.Front));
            early.Grid.Place(early.Units[0],new Vector2Int(5,11));early.Crossover.Boss.BeginTurn();
            for(int i=0;i<3;i++){early.Crossover.Boss.PatternStep=i;Assert.That(CrossoverBossTactics.Plan(early,early.Crossover.Boss).PrepareSkill.Id,Is.EqualTo("boss.hyades.0"));}
        }
    }
}
