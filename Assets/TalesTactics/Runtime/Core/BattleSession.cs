using System;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public sealed class BattleSession
    {
        public readonly GridMap Grid;
        public readonly List<UnitRuntime> Units=new List<UnitRuntime>();
        public readonly BattleRules Rules;
        public readonly SkillResolver Resolver;
        public ITurnScheduler Scheduler=new SpeedTurnScheduler();
        public IVictoryCondition Victory=new EliminateEnemies();
        public UnitRuntime Active;
        public BattleResult Result=>Victory.Evaluate(Units);
        public BattleSession(BattleCatalog catalog,IEnumerable<int> deployment,int level=1,int? enemyLevel=null,int campaignStage=-1)
        {
            Rules=catalog.Rules;Grid=campaignStage<0?GridMap.TestStage():CampaignContent.Map(campaignStage);int i=0;
            foreach(int index in deployment)
            {
                if(i>=Rules.MaxDeployment)break;
                var u=new UnitRuntime(catalog.Characters[index],Team.Player,Rules,level);Units.Add(u);Grid.Place(u,campaignStage<0?new Vector2Int(1+i%2,1+i/2):CampaignContent.PlayerSpawn(campaignStage,i));i++;
            }
            for(i=0;i<4;i++){var u=new UnitRuntime(catalog.Enemy,Team.Enemy,Rules,enemyLevel??level);u.Facing=Facing.Front;Units.Add(u);Grid.Place(u,campaignStage<0?new Vector2Int(7+i%2,5+i/2):CampaignContent.EnemySpawn(campaignStage,i));}
            Resolver=new SkillResolver(Grid,Units,Rules);
        }
        public void Advance(){Active=Scheduler.Next(Units);Active?.BeginTurn();}
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
