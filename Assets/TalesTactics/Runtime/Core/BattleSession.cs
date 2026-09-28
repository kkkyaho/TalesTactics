using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed class BattleSession
    {
        public readonly GridMap Grid;
        public readonly int CampaignStage;
        public readonly List<UnitRuntime> Units=new List<UnitRuntime>();
        public readonly BattleRules Rules;
        public readonly SkillResolver Resolver;
        public ITurnScheduler Scheduler=new SpeedTurnScheduler();
        public IVictoryCondition Victory=new EliminateEnemies();
        public UnitRuntime Active;
        public readonly bool UseUtilityAI;
        public readonly ObjectiveKind Objective;
        public readonly Vector2Int Destination=new Vector2Int(8,8);
        public UnitRuntime ObjectiveUnit {get;private set;}
        bool turnEnded;
        public string ObjectiveDescription=>Objective==ObjectiveKind.Reach?"아군 1명 목표 (8,8) 도착":
            Objective==ObjectiveKind.Escort?ObjectiveUnit.Data.DisplayName+" 호위 → (8,8), 전투불능 시 패배":
            Objective==ObjectiveKind.Boss?"보스: "+ObjectiveUnit.Data.DisplayName+" (첫 번째 적)":
            Objective==ObjectiveKind.Survive?"아군 턴 종료 "+((SurviveTurns)Victory).Completed+" / 12회 생존":"모든 적 격파";
        public BattleResult Result=>Victory.Evaluate(Units);
        public BattleSession(BattleCatalog catalog,IEnumerable<int> deployment,int level=1,int? enemyLevel=null,int campaignStage=-1,bool useCT=false,bool utilityAI=false,ObjectiveKind objective=ObjectiveKind.Eliminate)
        {
            if(objective!=ObjectiveKind.Eliminate&&campaignStage>=0)throw new ArgumentException("Special objectives require the training map");
            CampaignStage=campaignStage;Objective=objective;UseUtilityAI=utilityAI;if(useCT)Scheduler=new CTTurnScheduler();
            Rules=catalog.Rules;Grid=campaignStage<0?GridMap.TestStage():CampaignContent.Map(campaignStage);int i=0;
            foreach(int index in deployment)
            {
                if(i>=Rules.MaxDeployment)break;
                var u=new UnitRuntime(catalog.Characters[index],Team.Player,Rules,level);Units.Add(u);Grid.Place(u,campaignStage<0?new Vector2Int(1+i%2,1+i/2):CampaignContent.PlayerSpawn(campaignStage,i));i++;
            }
            for(i=0;i<4;i++){var u=new UnitRuntime(CampaignEnemies.Resolve(catalog,campaignStage,i),Team.Enemy,Rules,enemyLevel??level);u.Facing=Facing.Front;Units.Add(u);Grid.Place(u,campaignStage<0?new Vector2Int(7+i%2,5+i/2):CampaignContent.EnemySpawn(campaignStage,i));}
            Resolver=new SkillResolver(Grid,Units,Rules);
            if(utilityAI&&campaignStage<0)foreach(var enemy in Units.Where(u=>u.Team==Team.Enemy))
                enemy.TacticalSkills=catalog.Characters.SelectMany(c=>c.Skills).Where(s=>s!=null&&(s.Id=="mint.0"||s.Id=="jade.0")).ToArray();
            if(objective==ObjectiveKind.Boss){ObjectiveUnit=Units.First(u=>u.Team==Team.Enemy);ObjectiveUnit.Level=Math.Min(50,ObjectiveUnit.Level+5);ObjectiveUnit.CurrentHP=ObjectiveUnit.Stats.HP;ObjectiveUnit.CurrentMP=ObjectiveUnit.Stats.MP;Victory=new DefeatBoss(ObjectiveUnit);}
            if(objective==ObjectiveKind.Reach)Victory=new ReachDestination(Destination);
            if(objective==ObjectiveKind.Escort){ObjectiveUnit=Units.First(u=>u.Team==Team.Player);Victory=new ReachDestination(Destination,ObjectiveUnit);}
            if(objective==ObjectiveKind.Survive)Victory=new SurviveTurns(12);
        }
        public void Advance(){Active=Scheduler.Next(Units);turnEnded=false;Active?.BeginTurn();}
        public void EndTurn(){if(turnEnded||Active==null)return;Active.EndTurn();turnEnded=true;if(Victory is SurviveTurns survive)survive.OnTurnEnded(Active);}
        public bool Move(Vector2Int p)
        {
            var u=Active;if(u==null||u.Moved||!u.Alive||Grid.Path(u,p).Count<2)return false;
            u.MoveOrigin=u.Position;u.OriginalFacing=u.Facing;u.Facing=SkillResolver.Toward(u.Position,p);
            if(!Grid.Place(u,p))return false;u.Moved=true;u.CanUndoMove=true;return true;
        }
        public bool UndoMove()
        {
            var u=Active;if(u==null||!u.CanUndoMove||!Grid.Place(u,u.MoveOrigin))return false;
            u.Moved=false;u.CanUndoMove=false;u.Facing=u.OriginalFacing;return true;
        }
    }
}
