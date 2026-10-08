using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace TalesTactics
{
    // Selection never consumes a turn. Only Complete removes a unit from its phase.
    public sealed class TeamTurnScheduler : ITurnScheduler
    {
        readonly List<UnitRuntime> pending=new List<UnitRuntime>();
        readonly HashSet<UnitRuntime> begun=new HashSet<UnitRuntime>();
        public Team Phase {get;private set;}=Team.Player;
        public int Round {get;private set;}
        public bool CanSelect(UnitRuntime unit)=>unit!=null&&unit.Alive&&unit.Team==Phase&&pending.Contains(unit);
        public bool Begin(UnitRuntime unit)=>begun.Add(unit);
        public bool HasBegun(UnitRuntime unit)=>begun.Contains(unit);
        public void Complete(UnitRuntime unit){pending.Remove(unit);}
        public UnitRuntime Next(IReadOnlyList<UnitRuntime> units)
        {
            if(!units.Any(u=>u.Alive))return null;
            foreach(var unit in pending.Where(u=>!u.Alive))begun.Add(unit);
            pending.RemoveAll(u=>!u.Alive||!units.Contains(u));
            while(pending.Count==0)
            {
                if(Round==0)Round=1;
                else if(Phase==Team.Player)Phase=Team.Enemy;
                else{Phase=Team.Player;Round++;}
                begun.Clear();
                pending.AddRange(units.Where(u=>u.Alive&&u.Team==Phase).OrderByDescending(u=>u.Stats.SPD));
                // A unit absent at phase start waits for the next phase if revived later.
                foreach(var unit in units.Where(u=>!u.Alive&&u.Team==Phase))begun.Add(unit);
            }
            return pending[0];
        }
        public IReadOnlyList<UnitRuntime> Preview(IReadOnlyList<UnitRuntime> units)=>pending.Where(u=>u.Alive).ToArray();
        public int[] CapturePending(IReadOnlyList<UnitRuntime> units){var roster=units.ToArray();return pending.Select(u=>Array.IndexOf(roster,u)).ToArray();}
        public int[] CaptureBegun(IReadOnlyList<UnitRuntime> units){var roster=units.ToArray();return begun.Select(u=>Array.IndexOf(roster,u)).OrderBy(i=>i).ToArray();}
        public void Restore(IReadOnlyList<UnitRuntime> units,int[] order,int[] started,int round,UnitRuntime active)
        {
            // Suspension is only allowed at a player command, so the phase must be Player.
            var roster=units.ToArray();int activeIndex=Array.IndexOf(roster,active);
            bool Invalid(int[] ids)=>ids==null||ids.Distinct().Count()!=ids.Length||ids.Any(i=>i<0||i>=units.Count||units[i].Team!=Team.Player);
            if(round<1||Invalid(order)||Invalid(started)||!order.Contains(activeIndex)||!started.Contains(activeIndex))throw new InvalidDataException("Invalid player phase");
            var tracked=new HashSet<int>(order.Concat(started));
            if(roster.Where((u,i)=>u.Team==Team.Player&&u.Alive&&!tracked.Contains(i)).Any())throw new InvalidDataException("Missing phase unit");
            Phase=Team.Player;Round=round;pending.Clear();begun.Clear();
            pending.AddRange(order.Select(i=>units[i]));foreach(int i in started)begun.Add(units[i]);
        }
    }
}
