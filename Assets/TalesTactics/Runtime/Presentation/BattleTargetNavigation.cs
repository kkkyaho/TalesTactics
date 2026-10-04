using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        public Vector2Int[] AvailableTargets()
        {
            if(!(State is TargetSelectionState)||Session?.Active==null||SelectedSkill==null)return new Vector2Int[0];
            var u=Session.Active;
            return Session.Units.Select(t=>t.Position).Distinct().Where(p=>Session.Resolver.InRange(u,SelectedSkill,p)&&Session.Resolver.Targets(u,SelectedSkill,p).Any()).OrderBy(p=>p.x).ThenBy(p=>p.y).ToArray();
        }
        public void CycleTarget(int direction)
        {
            if(Hud.UnitDetailsOpen)return;
            var targets=AvailableTargets();if(targets.Length==0)return;
            int index=Target.HasValue?System.Array.IndexOf(targets,Target.Value):-1;
            index=index<0?(direction<0?targets.Length-1:0):(index+direction+targets.Length)%targets.Length;
            SelectTarget(targets[index]);Board.FocusCurrent();
        }
    }
}
