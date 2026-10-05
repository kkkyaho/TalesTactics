using System.Collections.Generic;
using System.Linq;
namespace TalesTactics
{
    public interface ITurnScheduler { UnitRuntime Next(IReadOnlyList<UnitRuntime> units); IReadOnlyList<UnitRuntime> Preview(IReadOnlyList<UnitRuntime> units); }
    public sealed class SpeedTurnScheduler : ITurnScheduler
    {
        readonly Queue<UnitRuntime> queue=new Queue<UnitRuntime>();
        public int Round {get;private set;}
        public int[] Capture(IReadOnlyList<UnitRuntime> units)=>queue.Select(u=>System.Array.IndexOf(units.ToArray(),u)).ToArray();
        public void Restore(IReadOnlyList<UnitRuntime> units,int[] order,int round){queue.Clear();foreach(int i in order)queue.Enqueue(units[i]);Round=round;}
        public UnitRuntime Next(IReadOnlyList<UnitRuntime> units)
        {
            while(queue.Count>0){var u=queue.Dequeue();if(u.Alive)return u;}
            var living=units.Where(u=>u.Alive).OrderByDescending(u=>u.Stats.SPD).ToArray();
            if(living.Length==0)return null;Round++;foreach(var u in living)queue.Enqueue(u);return queue.Dequeue();
        }
        public IReadOnlyList<UnitRuntime> Preview(IReadOnlyList<UnitRuntime> units)=>queue.Where(u=>u.Alive).Concat(units.Where(u=>u.Alive).OrderByDescending(u=>u.Stats.SPD)).Take(8).ToArray();
    }
    public interface IVictoryCondition { BattleResult Evaluate(IReadOnlyList<UnitRuntime> units); }
    public enum BattleResult { Ongoing, Victory, Defeat }
    public sealed class EliminateEnemies : IVictoryCondition
    {
        public BattleResult Evaluate(IReadOnlyList<UnitRuntime> units)
        {if(!units.Any(u=>u.Alive&&u.Team==Team.Player))return BattleResult.Defeat;return units.Any(u=>u.Alive&&u.Team==Team.Enemy)?BattleResult.Ongoing:BattleResult.Victory;}
    }
}
