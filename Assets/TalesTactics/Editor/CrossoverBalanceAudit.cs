using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Editor
{
    // Legal-command simulation, separate from objective fixture tests and graphical UI review.
    public static class CrossoverBalanceAudit
    {
        public static void RunBatch()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            var lines=new List<string>{"episode,result,turns,moves,skills,survivors,playerLevel,enemyLevel"};
            var preferred=new[]{"cless","mint","alphen","shionne","jade","kisara","farah","tear","velvet","natalia"};
            for(int stage=6;stage<=25;stage++)
            {
                int episode=stage-5;var chapter=CampaignStages.Get(stage);
                var deploy=preferred.Where(id=>CrossoverCampaign.Joined(id,episode)).Take(catalog.Rules.MaxDeployment).Select(id=>Array.FindIndex(catalog.Characters,c=>c.Id==id)).ToArray();
                var b=new BattleSession(catalog,deploy,chapter.EntryLevel,chapter.EnemyLevel,stage,teamTurns:true,tacticalCombat:true,reactions:true,actionExperience:false);
                int turns=0,moves=0,skills=0;
                while(b.Result==BattleResult.Ongoing&&turns++<500)
                {
                    b.Advance();var actor=b.Active;if(actor==null)break;
                    if(actor.Has(StatusKind.Stun)||actor.Has(StatusKind.Sleep)){b.EndTurn();continue;}
                    var plan=actor.Team==Team.Enemy?new EnemyPlanner().Plan(b,actor):new UtilityPlanner().Plan(b,actor);
                    if(actor.Team==Team.Player&&!b.Units.Any(u=>u.Team==Team.Enemy&&u.Alive&&!u.CrossoverReserve))
                    {
                        Vector2Int? goal=null;
                        if(b.Crossover.UsesNodes&&b.Crossover.State.Nodes!=7)goal=episode==13&&b.Crossover.State.Nodes==0?CrossoverBattle.Nodes[b.Crossover.State.Choice]:CrossoverBattle.Nodes.Where((p,i)=>(b.Crossover.State.Nodes&(1<<i))==0).OrderBy(p=>GridMap.Distance(actor.Position,p)).First();
                        else if(b.Crossover.UsesExit)goal=CrossoverBattle.Exit;
                        if(goal.HasValue)plan=new EnemyPlan{Destination=Approach(b,actor,goal.Value),Guard=true};
                    }
                    EnemyTactics.Commit(actor,plan,b);
                    if(plan.Destination!=actor.Position&&b.Move(plan.Destination))moves++;
                    if(plan.Skill!=null&&(plan.Aim.HasValue||plan.Target!=null))
                    {
                        var aim=plan.Aim??plan.Target.Position;actor.Facing=SkillResolver.Toward(actor.Position,aim);
                        if(b.Resolver.Execute(actor,plan.Skill,aim,out _))skills++;
                    }
                    else if(plan.Guard&&!actor.Acted){actor.Acted=true;actor.AddStatus(StatusKind.Guard,2);}
                    b.EndTurn();
                }
                string line=string.Join(",",episode,b.Result,turns,moves,skills,b.Units.Count(u=>u.Team==Team.Player&&u.Alive),chapter.EntryLevel,chapter.EnemyLevel);
                lines.Add(line);Debug.Log("CROSSOVER_BALANCE "+line);
                File.WriteAllLines("Docs/CrossoverStatus/balance-audit.csv",lines);
            }
        }
        static Vector2Int Approach(BattleSession b,UnitRuntime actor,Vector2Int goal)
        {
            var distance=new Dictionary<Vector2Int,int>{{goal,0}};var queue=new Queue<Vector2Int>();queue.Enqueue(goal);
            while(queue.Count>0)
            {
                var p=queue.Dequeue();foreach(var tile in b.Grid.Neighbors(p))
                {
                    if(!tile.Walkable||Math.Abs(tile.Height-b.Grid[p].Height)>actor.Stats.JMP||tile.Occupant!=null&&tile.Occupant!=actor||distance.ContainsKey(tile.Coordinate))continue;
                    distance[tile.Coordinate]=distance[p]+1;queue.Enqueue(tile.Coordinate);
                }
            }
            return b.Grid.Reachable(actor,out _).Keys.Where(distance.ContainsKey).OrderBy(p=>distance[p]).ThenBy(p=>GridMap.Distance(actor.Position,p)).FirstOrDefault();
        }
    }
}
