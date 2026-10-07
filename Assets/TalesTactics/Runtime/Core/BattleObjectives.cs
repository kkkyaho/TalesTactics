using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public enum ObjectiveKind { Eliminate, Boss, Reach, Escort, Survive }
    public sealed class DefeatBoss : IVictoryCondition
    {
        readonly UnitRuntime boss;
        public DefeatBoss(UnitRuntime boss){this.boss=boss??throw new ArgumentNullException(nameof(boss));}
        public BattleResult Evaluate(IReadOnlyList<UnitRuntime> units)=>!units.Any(u=>u.Team==Team.Player&&u.Alive)?BattleResult.Defeat:
            !boss.Alive?BattleResult.Victory:BattleResult.Ongoing;
    }
    public sealed class ReachDestination : IVictoryCondition
    {
        readonly Vector2Int destination;readonly UnitRuntime escort;
        public ReachDestination(Vector2Int destination,UnitRuntime escort=null){this.destination=destination;this.escort=escort;}
        public BattleResult Evaluate(IReadOnlyList<UnitRuntime> units)
        {
            if(!units.Any(u=>u.Team==Team.Player&&u.Alive)||escort!=null&&!escort.Alive)return BattleResult.Defeat;
            return units.Any(u=>u.Team==Team.Player&&u.Alive&&u.Position==destination&&(escort==null||u==escort))?BattleResult.Victory:BattleResult.Ongoing;
        }
    }
    public sealed class SurviveTurns : IVictoryCondition
    {
        public int Completed {get;private set;}
        public readonly int Required;
        public SurviveTurns(int required){if(required<1)throw new ArgumentOutOfRangeException(nameof(required));Required=required;}
        public void OnTurnEnded(UnitRuntime unit){if(unit!=null&&unit.Team==Team.Player&&unit.Alive)Completed++;}
        public void Restore(int completed){if(completed<0||completed>=Required)throw new ArgumentOutOfRangeException(nameof(completed));Completed=completed;}
        public BattleResult Evaluate(IReadOnlyList<UnitRuntime> units)=>!units.Any(u=>u.Team==Team.Player&&u.Alive)?BattleResult.Defeat:
            Completed>=Required?BattleResult.Victory:BattleResult.Ongoing;
    }
    public static class ObjectiveNames
    {
        public static string Name(ObjectiveKind kind)=>kind==ObjectiveKind.Boss?"보스 격파":kind==ObjectiveKind.Reach?"목표 도착":
            kind==ObjectiveKind.Escort?"호위":kind==ObjectiveKind.Survive?"생존":"모든 적 격파";
    }
}
