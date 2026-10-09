using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleSession
    {
        public readonly GridMap Grid;
        public readonly int CampaignStage;
        public readonly bool LegacyCampaign;
        public readonly bool TacticalCombat;
        public readonly bool BossEncounters;
        public readonly List<UnitRuntime> Units=new List<UnitRuntime>();
        public readonly BattleRules Rules;
        public readonly SkillResolver Resolver;
        public ITurnScheduler Scheduler=new SpeedTurnScheduler();
        public IVictoryCondition Victory=new EliminateEnemies();
        public UnitRuntime Active;
        public BattleOpening Opening;
        public readonly bool UseUtilityAI;
        public readonly ObjectiveKind Objective;
        public readonly Vector2Int Destination=new Vector2Int(8,8);
        public UnitRuntime ObjectiveUnit {get;private set;}
        bool turnEnded;
        public string ObjectiveDescription=>CaptureMission?"거점 점령 "+CaptureProgress+" / 2 · 아군 턴 종료 시 유지":Objective==ObjectiveKind.Reach?"아군 1명 목표 "+Destination+" 도착":
            Objective==ObjectiveKind.Escort?ObjectiveUnit.Data.DisplayName+" 호위 → "+Destination+", 전투불능 시 패배":
            Objective==ObjectiveKind.Boss?"보스: "+ObjectiveUnit.Data.DisplayName+" (첫 번째 적)":
            Objective==ObjectiveKind.Survive?SurvivalDescription:"모든 적 격파";
        string SurvivalDescription {get{var goal=(SurviveTurns)Victory;return (goal.EnemyPhases?"적군 턴 생존 ":"아군 턴 종료 ")+goal.Completed+" / "+goal.Required;}}
        public BattleResult Result=>Victory.Evaluate(Units);
        public BattleSession(BattleCatalog catalog,IEnumerable<int> deployment,int level=1,int? enemyLevel=null,int campaignStage=-1,bool useCT=false,bool utilityAI=false,ObjectiveKind objective=ObjectiveKind.Eliminate,bool legacyCampaign=false,bool teamTurns=false,bool phaseSurvival=false,bool tacticalCombat=false,bool bossEncounters=false,BattleDifficulty difficulty=BattleDifficulty.Standard,bool missionEvents=false)
        {
            if(!Enum.IsDefined(typeof(BattleDifficulty),difficulty))throw new ArgumentOutOfRangeException(nameof(difficulty));
            this.catalog=catalog;Difficulty=campaignStage<0?BattleDifficulty.Standard:difficulty;MissionEvents=missionEvents&&campaignStage>=0&&!legacyCampaign&&teamTurns&&!useCT;
            if(campaignStage>=0)enemyLevel=DifficultyRules.EnemyLevel(enemyLevel??level,Difficulty);
            if(campaignStage>=0&&!legacyCampaign){objective=CampaignMissions.Kind(campaignStage);Destination=CampaignMissions.Destination(campaignStage);}
            BossEncounters=bossEncounters&&teamTurns&&!useCT&&!legacyCampaign&&(campaignStage==1||campaignStage==5);
            TacticalCombat=tacticalCombat;CampaignStage=campaignStage;LegacyCampaign=legacyCampaign;Objective=objective;UseUtilityAI=utilityAI;if(useCT)Scheduler=new CTTurnScheduler();else if(teamTurns)Scheduler=new TeamTurnScheduler();
            Rules=catalog.Rules;Grid=campaignStage<0?GridMap.TestStage():CampaignContent.Map(campaignStage);int i=0;
            foreach(int index in deployment)
            {
                if(i>=Rules.MaxDeployment)break;
                var u=new UnitRuntime(catalog.Characters[index],Team.Player,Rules,level);Units.Add(u);Grid.Place(u,campaignStage<0?new Vector2Int(1+i%2,1+i/2):CampaignContent.PlayerSpawn(campaignStage,i));i++;
            }
            for(i=0;i<4;i++){var u=new UnitRuntime(CampaignEnemies.Resolve(catalog,campaignStage,i),Team.Enemy,Rules,enemyLevel??level);u.Facing=Facing.Front;Units.Add(u);Grid.Place(u,campaignStage<0?new Vector2Int(7+i%2,5+i/2):CampaignContent.EnemySpawn(campaignStage,i));}
            Resolver=new SkillResolver(Grid,Units,Rules,tacticalCombat:tacticalCombat);
            if(utilityAI&&campaignStage<0)foreach(var enemy in Units.Where(u=>u.Team==Team.Enemy))
                enemy.TacticalSkills=catalog.Characters.SelectMany(c=>c.Skills).Where(s=>s!=null&&(s.Id=="mint.0"||s.Id=="jade.0")).ToArray();
            if(campaignStage>=0&&!legacyCampaign)EnemyTactics.Configure(this,catalog);
            if(objective==ObjectiveKind.Boss){ObjectiveUnit=Units.FirstOrDefault(u=>u.Team==Team.Enemy&&u.Data.Id=="dhaos")??Units.First(u=>u.Team==Team.Enemy);if(campaignStage<0)ObjectiveUnit.Level=Math.Min(50,ObjectiveUnit.Level+5);ObjectiveUnit.CurrentHP=ObjectiveUnit.Stats.HP;ObjectiveUnit.CurrentMP=ObjectiveUnit.Stats.MP;Victory=new DefeatBoss(ObjectiveUnit);}
            if(BossEncounters)
            {
                ObjectiveUnit.BossWard=true;ObjectiveUnit.Trait=TacticalTrait.Balanced;
                var enemies=Units.Where(u=>u.Team==Team.Enemy).ToArray();
                foreach(var enemy in enemies)Grid[enemy.Position].Occupant=null;
                var positions=campaignStage==1?new[]{new Vector2Int(6,9),new Vector2Int(4,6),new Vector2Int(7,6),new Vector2Int(6,7)}:new[]{new Vector2Int(6,12),new Vector2Int(2,9),new Vector2Int(10,9),new Vector2Int(6,9)};
                for(int n=0;n<enemies.Length;n++)Grid.Place(enemies[n],positions[n]);
            }
            if(objective==ObjectiveKind.Reach)Victory=new ReachDestination(Destination);
            if(CaptureMission)Victory=new CaptureDestination(this);
            if(objective==ObjectiveKind.Escort){ObjectiveUnit=Units.First(u=>u.Team==Team.Player);Victory=new ReachDestination(Destination,ObjectiveUnit);}
            if(objective==ObjectiveKind.Survive)Victory=new SurviveTurns(phaseSurvival&&teamTurns&&!useCT?4:12,phaseSurvival&&teamTurns&&!useCT);
        }
        void BeginActive(){turnEnded=false;if(Active!=null&&(!(Scheduler is TeamTurnScheduler team)||team.Begin(Active)))Active.BeginTurn();}
        public void Advance(){Active=Scheduler.Next(Units);AdvanceMissionEvents();BeginActive();}
        public bool CanSelect(UnitRuntime unit)=>!turnEnded&&Active?.Team==Team.Player&&Scheduler is TeamTurnScheduler team&&team.Phase==Team.Player&&unit?.Team==Team.Player&&team.CanSelect(unit);
        public bool Select(UnitRuntime unit){if(!CanSelect(unit))return false;Active=unit;BeginActive();return true;}
        public void EndTurn()
        {
            if(turnEnded||Active==null)return;Active.EndTurn();turnEnded=true;
            if(Scheduler is TeamTurnScheduler team)
            {
                team.Complete(Active);
                CompleteMissionPhase(team);
                if(Victory is SurviveTurns goal&&goal.EnemyPhases&&team.Preview(Units).Count==0&&
                    (team.Phase==Team.Enemy||!Units.Any(u=>u.Alive&&u.Team==Team.Enemy)))goal.OnEnemyPhaseEnded();
            }
            if(Victory is SurviveTurns survive)survive.OnTurnEnded(Active);
        }
        public int RemainingAllies=>Scheduler is TeamTurnScheduler team&&team.Phase==Team.Player?Units.Count(team.CanSelect):0;
        public string ActionState(UnitRuntime unit)
        {
            if(unit==null||!unit.Alive)return "전투불능";
            if(!(Scheduler is TeamTurnScheduler team))return unit==Active?"선택":"대기";
            if(team.Phase!=Team.Player||!team.CanSelect(unit))return "완료";
            if(!team.HasBegun(unit))return "이동·행동";
            bool action=!unit.Acted||unit.FlamingChain;
            return !unit.Moved&&action?"이동·행동":!unit.Moved?"이동":action?"행동":"대기 가능";
        }
        public bool EndPlayerPhase()
        {
            if(turnEnded||Active?.Team!=Team.Player||!(Scheduler is TeamTurnScheduler team)||team.Phase!=Team.Player||Result!=BattleResult.Ongoing)return false;
            foreach(var unit in Units.Where(team.CanSelect).ToArray())
            {
                Active=unit;BeginActive();EndTurn();
                if(Result!=BattleResult.Ongoing)break;
            }
            return true;
        }
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
