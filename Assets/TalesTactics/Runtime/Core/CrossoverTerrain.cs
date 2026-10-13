using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    [Serializable] public sealed class CrossoverHazard
    {
        public int X,Y,Owner,Expires;
        public int TriggerTurn;
        public bool Triggered;
        public BossMechanic Kind;
        public Vector2Int Position=>new Vector2Int(X,Y);
        public CrossoverHazard Copy()=>new CrossoverHazard{X=X,Y=Y,Owner=Owner,Expires=Expires,Kind=Kind,TriggerTurn=TriggerTurn,Triggered=Triggered};
    }
    public sealed partial class CrossoverBattle
    {
        readonly HashSet<Vector2Int> closed=new HashSet<Vector2Int>();
        readonly Dictionary<Vector2Int,int> rubbleCosts=new Dictionary<Vector2Int,int>();
        public void OnResolved(UnitRuntime actor,SkillData skill,Vector2Int aim)
        {
            RecordTrialAction(actor,skill);
            if(actor.Team==Team.Player)
            {State.LastShape=(int)skill.Shape;State.LastArea=Math.Min(4,skill.Area);State.LastRange=Math.Min(6,skill.Range);return;}
            if(!actor.CrossoverBoss)return;
            var kind=skill.BossMechanic;
            if(kind!=BossMechanic.FlameField&&kind!=BossMechanic.IceTrail&&kind!=BossMechanic.Mines&&kind!=BossMechanic.Boundary&&kind!=BossMechanic.Collapse&&kind!=BossMechanic.GaugeSeal)return;
            int owner=battle.Units.IndexOf(actor);
            foreach(var p in battle.Resolver.AreaTiles(actor,skill,aim))
            {
                var t=battle.Grid[p];
                // Keep rescue/return routes open and leave alternate lanes every second tile.
                if(t==null||!t.Walkable||Nodes.Contains(p)||p==Exit||kind==BossMechanic.Boundary&&(p.x+p.y)%2==0)continue;
                State.Hazards.RemoveAll(h=>h.Position==p&&h.Kind==kind);
                State.Hazards.Add(new CrossoverHazard{X=p.x,Y=p.y,Kind=kind,Owner=owner,Expires=actor.TurnsStarted+(kind==BossMechanic.Collapse?3:2),TriggerTurn=kind==BossMechanic.Collapse?actor.TurnsStarted+1:0});
            }
            RefreshBoundaries();
        }
        void ValidateHazards()
        {
            if(State.Hazards==null||State.Hazards.Count>169*6||!Enum.IsDefined(typeof(SkillAreaShape),State.LastShape)||!Enum.IsDefined(typeof(SkillAreaShape),State.ReplayShape)||State.LastArea<0||State.LastArea>4||State.ReplayArea<0||State.ReplayArea>4||State.LastRange<0||State.LastRange>6)throw new System.IO.InvalidDataException("Invalid crossover terrain state");
            if(State.TrialProgress<0||State.TrialProgress>7||State.TrialActions<0||State.TrialActions>100000)throw new System.IO.InvalidDataException("Invalid personal trial progress");
            if(State.BrokenDevices.Count>3||State.BrokenDevices.Distinct().Count()!=State.BrokenDevices.Count||State.BrokenDevices.Any(i=>i<0||i>=battle.Units.Count||battle.Units[i].Data.Id!="boundary-device"||battle.Units[i].Alive))throw new System.IO.InvalidDataException("Invalid device destruction history");
            if(IsTrial&&TrialOwner=="cless"&&State.Nodes!=0&&State.Nodes!=1&&State.Nodes!=3&&State.Nodes!=7)throw new System.IO.InvalidDataException("Invalid contract signal order");
            foreach(var h in State.Hazards)
                if(h==null||battle.Grid[h.Position]==null||h.Owner<0||h.Owner>=battle.Units.Count||!battle.Units[h.Owner].CrossoverBoss||h.Expires<0||h.Expires>100000||h.TriggerTurn<0||h.TriggerTurn>h.Expires||
                    h.Kind!=BossMechanic.FlameField&&h.Kind!=BossMechanic.IceTrail&&h.Kind!=BossMechanic.Mines&&h.Kind!=BossMechanic.Boundary&&h.Kind!=BossMechanic.Collapse&&h.Kind!=BossMechanic.GaugeSeal)
                    throw new System.IO.InvalidDataException("Invalid crossover hazard");
        }
        public void OnMoved(UnitRuntime unit,IReadOnlyList<Vector2Int> path)
        {
            foreach(var h in State.Hazards.Where(h=>h.Kind==BossMechanic.Mines&&path.Skip(1).Contains(h.Position)&&unit.Team!=battle.Units[h.Owner].Team).ToArray())
            {unit.Damage(Math.Max(1,unit.Stats.HP/8),battle.Grid);State.Hazards.Remove(h);unit.CanUndoMove=false;}
            RefreshBoundaries();Update();
        }
        void ApplyHazards(UnitRuntime unit)
        {
            RecordTrialGuard(unit);
            foreach(var h in State.Hazards.Where(h=>h.Kind==BossMechanic.Collapse&&!h.Triggered&&battle.Units[h.Owner]==unit&&unit.TurnsStarted>=h.TriggerTurn))
            {
                h.Triggered=true;var target=battle.Grid[h.Position].Occupant;
                if(target!=null&&target.Team!=unit.Team)target.Damage(Math.Max(1,target.Stats.HP/8),battle.Grid);
            }
            foreach(var h in State.Hazards.Where(h=>h.Position==unit.Position&&unit.Team!=battle.Units[h.Owner].Team).ToArray())
            {
                if(h.Kind==BossMechanic.FlameField)unit.Damage(Math.Max(1,unit.Stats.HP/10),battle.Grid);
                if(h.Kind==BossMechanic.IceTrail)unit.AddStatus(StatusKind.Root,1);
            }
            State.Hazards.RemoveAll(h=>!battle.Units[h.Owner].Alive||battle.Units[h.Owner]==unit&&unit.TurnsStarted>=h.Expires||h.Kind==BossMechanic.GaugeSeal&&DevicesDown);
            RefreshBoundaries();
        }
        void RefreshBoundaries()
        {
            foreach(var pair in rubbleCosts)battle.Grid[pair.Key].MovementCost=pair.Value;rubbleCosts.Clear();
            foreach(var p in State.Hazards.Where(h=>h.Kind==BossMechanic.Collapse&&h.Triggered).Select(h=>h.Position).Distinct())
            {rubbleCosts[p]=battle.Grid[p].MovementCost;battle.Grid[p].MovementCost=Math.Max(2,battle.Grid[p].MovementCost);}
            foreach(var p in closed)battle.Grid[p].Walkable=true;closed.Clear();
            foreach(var h in State.Hazards.Where(h=>h.Kind==BossMechanic.Boundary))
                if(battle.Grid[h.Position].Occupant==null){battle.Grid[h.Position].Walkable=false;closed.Add(h.Position);}
        }
    }
}
