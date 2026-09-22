using System;
using System.Collections.Generic;
using System.Linq;
namespace TalesTactics
{
    // Integer CT ticks. Ties use deployment order; excess CT carries into the next turn.
    public sealed class CTTurnScheduler : ITurnScheduler
    {
        const long Threshold=1000;
        readonly Dictionary<UnitRuntime,long> charge=new Dictionary<UnitRuntime,long>();
        public UnitRuntime Next(IReadOnlyList<UnitRuntime> units)=>Next(units,charge);
        static UnitRuntime Next(IReadOnlyList<UnitRuntime> units,Dictionary<UnitRuntime,long> state)
        {
            foreach(var dead in state.Keys.Where(u=>!u.Alive||!units.Contains(u)).ToArray())state.Remove(dead);
            var living=units.Where(u=>u.Alive).ToArray();if(living.Length==0)return null;
            foreach(var u in living)if(!state.ContainsKey(u))state[u]=0;
            long ticks=living.Min(u=>Math.Max(0,(Threshold-state[u]+Math.Max(1,u.Stats.SPD)-1)/Math.Max(1,u.Stats.SPD)));
            foreach(var u in living)state[u]+=ticks*Math.Max(1,u.Stats.SPD);
            var next=living.OrderByDescending(u=>state[u]).First();state[next]-=Threshold;return next;
        }
        public IReadOnlyList<UnitRuntime> Preview(IReadOnlyList<UnitRuntime> units)
        {
            var copy=new Dictionary<UnitRuntime,long>(charge);var result=new List<UnitRuntime>();
            for(int i=0;i<8;i++){var u=Next(units,copy);if(u==null)break;result.Add(u);}return result;
        }
    }
}
