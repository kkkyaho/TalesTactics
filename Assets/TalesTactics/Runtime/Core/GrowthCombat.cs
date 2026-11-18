using System.Collections.Generic;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class SkillResolver
    {
        // One action-local recipient set: no new saved state, extra actions, EXP, or reaction loops.
        string ResolveGrowthAction(UnitRuntime actor,HashSet<UnitRuntime> recipients)
        {
            if(recipients==null||!actor.Alive)return "";
            var notes=new List<string>();
            foreach(var target in recipients)
            {
                if(!target.Alive||target==actor||target.CrossoverReserve)continue;
                if(GrowthPaths.Active(actor,"cless",GrowthPath.Assault))
                {
                    if(!actor.Moved||actor.Position==actor.MoveOrigin||target.Team==actor.Team||target.CrossoverBoss||target.ExperienceBoss||target.BossWard||target.Data.Id=="boundary-device")continue;
                    var before=target.Position;Displace(target,actor.Position,1,true);
                    if(target.Position!=before)notes.Add(target.Data.DisplayName+" · 돌파 밀치기 1칸");
                }
                else if(target.Team==actor.Team)
                {
                    var status=actor.Growth==GrowthPath.Assault?StatusKind.Song:StatusKind.Guard;
                    // Guard survives this phase and expires at the recipient's next BeginTurn.
                    // Existing longer songs/guards are preserved; they never stack their multiplier.
                    target.AddStatus(status,status==StatusKind.Guard?2:1);
                    notes.Add(target.Data.DisplayName+" · "+(status==StatusKind.Song?"공격 지원: 성가 1턴":"방어 지원: 다음 자기 턴까지 방어"));
                }
            }
            return string.Join(" · ",notes);
        }
    }
}
