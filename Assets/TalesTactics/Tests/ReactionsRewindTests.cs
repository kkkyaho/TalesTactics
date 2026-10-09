using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        BattleSession ReactionBattle(out BattleCatalog catalog)
        {
            catalog=FreeTurnCatalog();var b=new BattleSession(catalog,new[]{0,1},campaignStage:0,teamTurns:true,reactions:true);
            foreach(var tile in b.Grid.Tiles.Values){tile.Occupant=null;}
            var positions=new[]{new Vector2Int(2,2),new Vector2Int(3,3),new Vector2Int(3,2),new Vector2Int(8,5),new Vector2Int(8,6),new Vector2Int(8,8)};
            for(int i=0;i<b.Units.Count;i++){b.Grid.Place(b.Units[i],positions[i]);b.Units[i].Data.BasicAttack.Range=1;}
            b.Opening=BattleOpening.Capture(b,catalog);b.Advance();b.Active.Facing=Facing.Right;return b;
        }
        [Test] public void ReactionsMatchForecastAndDoNotSpendNormalActionsOrMutatePreview()
        {
            var b=ReactionBattle(out _);var a=b.Active;var helper=b.Units[1];var target=b.Units[2];
            string before=JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1}));
            var f=BattleForecast.Create(b,a,a.Data.BasicAttack,target.Position);
            Assert.That(f.Reactions,Does.Contain("지원").And.Contain("반격"));Assert.That(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1})),Is.EqualTo(before));
            Assert.That(b.Resolver.Execute(a,a.Data.BasicAttack,target.Position,out _),Is.True);
            foreach(var row in f.Rows)Assert.That(row.AfterHP,Is.EqualTo(row.Unit.CurrentHP));
            Assert.That(helper.SupportUsed,Is.True);Assert.That(target.CounterUsed,Is.True);Assert.That(helper.Acted||target.Acted,Is.False);
            Assert.That(helper.SpecialGauge,Is.Zero);Assert.That(b.Resolver.LastReactions.Count,Is.EqualTo(2));
            a.Acted=false;b.Resolver.Execute(a,a.Data.BasicAttack,target.Position,out _);Assert.That(b.Resolver.LastReactions,Is.Empty);
            helper.BeginTurn();target.BeginTurn();Assert.That(helper.SupportUsed||target.CounterUsed,Is.False);
        }
        [Test] public void ReactionsRespectRangeStunDeathAreaAndLegacySwitch()
        {
            var b=ReactionBattle(out _);var a=b.Active;var t=b.Units[2];b.Units[1].AddStatus(StatusKind.Stun,2);t.AddStatus(StatusKind.Stun,2);
            b.Resolver.Execute(a,a.Data.BasicAttack,t.Position,out _);Assert.That(b.Resolver.LastReactions,Is.Empty);
            t.Statuses.Clear();b.Grid.Place(t,new Vector2Int(5,2));a.Acted=false;var ranged=Skill(EffectKind.Damage);b.Resolver.Execute(a,ranged,t.Position,out _);Assert.That(b.Resolver.LastReactions,Is.Empty);
            b.Grid.Place(t,new Vector2Int(3,2));a.Acted=false;ranged.Area=1;b.Resolver.Execute(a,ranged,t.Position,out _);Assert.That(b.Resolver.LastReactions,Is.Empty);
            a.Acted=false;t.CurrentHP=1;b.Resolver.Execute(a,a.Data.BasicAttack,t.Position,out _);Assert.That(b.Resolver.LastReactions,Is.Empty);
            var legacy=new BattleSession(FreeTurnCatalog(),new[]{0},teamTurns:true);Assert.That(legacy.Resolver.ReactionsEnabled,Is.False);
        }
        [Test] public void ReactionsSaveFlagsAndRetryResetsThemWithoutChangingRule()
        {
            var b=ReactionBattle(out var c);b.Resolver.Execute(b.Active,b.Active.Data.BasicAttack,b.Units[2].Position,out _);
            var cp=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1})));
            var restored=cp.Restore(c);Assert.That(restored.Resolver.ReactionsEnabled,Is.True);Assert.That(restored.Units[1].SupportUsed&&restored.Units[2].CounterUsed,Is.True);
            Assert.That(restored.Resolver.RandomDraws,Is.EqualTo(b.Resolver.RandomDraws));var retry=restored.Opening.Restore(c);
            Assert.That(retry.Resolver.ReactionsEnabled,Is.True);Assert.That(retry.Units.Any(u=>u.SupportUsed||u.CounterUsed),Is.False);
            cp.Version=7;Assert.That(cp.Restore(c).Resolver.ReactionsEnabled,Is.False);
        }
        [Test] public void RewindRestoresCompleteActionAndCombatRandomSequenceAfterSave()
        {
            var b=ReactionBattle(out var c);var s=Skill(EffectKind.Damage);s.Effects=new[]{new SkillEffect{Kind=EffectKind.Damage,Power=.2f},new SkillEffect{Kind=EffectKind.Status,Status=StatusKind.Cage,Chance=.5f,Duration=2}};
            b.Resolver.RestoreRandom(13);b.RecordAction("기술");b.Resolver.Execute(b.Active,s,b.Units[2].Position,out _);
            var expected=b.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))).ToArray();int draws=b.Resolver.RandomDraws;
            b=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1}))).Restore(c);
            var rewind=b.RewindLast(out _);Assert.That(rewind.RewindsLeft,Is.EqualTo(2));Assert.That(rewind.Active.Acted,Is.False);Assert.That(rewind.Resolver.RandomDraws,Is.EqualTo(13));
            rewind.Resolver.Execute(rewind.Active,s,rewind.Units[2].Position,out _);
            Assert.That(rewind.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))).ToArray(),Is.EqualTo(expected));Assert.That(rewind.Resolver.RandomDraws,Is.EqualTo(draws));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void RewindRestoresSchedulerAcrossEnemyTurnAndLimitsUses(int mode)
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0,1},campaignStage:0,teamTurns:mode==0,useCT:mode==2);b.Advance();
            for(int i=0;i<100&&b.Active.Team!=Team.Player;i++){b.EndTurn();b.Advance();}
            var before=JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1},false));b.RecordAction("대기");
            for(int i=0;i<20;i++){b.EndTurn();b.Advance();}b.Active.CurrentHP=1;
            b=b.RewindLast(out _);var restored=BattleCheckpoint.Capture(b,new[]{0,1},false);restored.RewindsUsed=0;
            Assert.That(JsonUtility.ToJson(restored),Is.EqualTo(before));
            for(int i=0;i<2;i++){b.RecordAction("이동");b.Active.CurrentMP=0;b=b.RewindLast(out _);}
            Assert.That(b.RewindsLeft,Is.Zero);b.RecordAction("불가");Assert.Throws<System.InvalidOperationException>(()=>b.RewindLast(out _));
        }
        [Test] public void RewindRestoresReinforcementRosterAndCaptureProgress()
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0,1},campaignStage:2,teamTurns:true,missionEvents:true);b.Advance();NextMissionRound(b);b.RecordAction("턴 종료");
            NextMissionRound(b);Assert.That(b.ReinforcementsArrived,Is.True);b=b.RewindLast(out _);Assert.That(b.ReinforcementsArrived,Is.False);Assert.That(b.Units.Count,Is.EqualTo(6));Assert.That(b.CaptureProgress,Is.Zero);
        }
        [Test] public void RewindRejectsNestedCorruptionAndKeepsHistoryBounded()
        {
            var b=ReactionBattle(out var c);for(int i=0;i<15;i++)b.RecordAction("이동");Assert.That(b.History.Count,Is.EqualTo(12));
            var cp=BattleCheckpoint.Capture(b,new[]{0,1});var nested=BattleCheckpoint.Capture(b,new[]{0,1});cp.History[0]=new RewindFrame{Label="bad",Json=JsonUtility.ToJson(nested)};
            Assert.Throws<InvalidDataException>(()=>cp.Restore(c));
            b.RewindsUsed=4;Assert.Throws<InvalidDataException>(()=>BattleCheckpoint.Capture(b,new[]{0,1}).Restore(c));
        }
    }
}
