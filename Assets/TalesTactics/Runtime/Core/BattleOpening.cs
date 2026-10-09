using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // Immutable-by-convention pre-turn snapshot. Contains no campaign rewards or scheduler progress.
    [Serializable] public sealed class BattleOpening
    {
        public int Version=3,Stage;
        public int[] Deployment;
        public ObjectiveKind Objective;
        public BattleDifficulty Difficulty;
        public bool MissionEvents,Reactions;
        public bool CT,TeamTurns,Utility,Legacy,PhaseSurvival,TacticalCombat,BossEncounters;
        public UnityEngine.Random.State RandomState;
        public List<CheckpointUnit> Units=new List<CheckpointUnit>();
        public static BattleOpening Capture(BattleSession session,BattleCatalog catalog)
        {
            if(session.Active!=null||session.Units.Any(u=>u.TurnsStarted!=0))throw new InvalidOperationException("Opening must precede the first turn");
            return new BattleOpening{Stage=session.CampaignStage,Objective=session.Objective,
                Deployment=session.Units.Where(u=>u.Team==Team.Player).Select(u=>Array.IndexOf(catalog.Characters,u.Data)).ToArray(),
                CT=session.Scheduler is CTTurnScheduler,TeamTurns=session.Scheduler is TeamTurnScheduler,Utility=session.UseUtilityAI,
                Legacy=session.LegacyCampaign,PhaseSurvival=(session.Victory as SurviveTurns)?.EnemyPhases??false,
                Reactions=session.Resolver.ReactionsEnabled,Difficulty=session.Difficulty,MissionEvents=session.MissionEvents,TacticalCombat=session.TacticalCombat,BossEncounters=session.BossEncounters,RandomState=UnityEngine.Random.state,
                Units=session.Units.Select(u=>new CheckpointUnit(u)).ToList()};
        }
        public BattleSession Restore(BattleCatalog catalog)
        {
            if((Version<1||Version>3)||Stage< -1||Stage>=CampaignStages.Count||CT&&TeamTurns||!Enum.IsDefined(typeof(ObjectiveKind),Objective)||Deployment==null||Deployment.Length<1||Deployment.Length>catalog.Rules.MaxDeployment||Deployment.Distinct().Count()!=Deployment.Length||Deployment.Any(i=>i<0||i>=catalog.Characters.Length))throw new InvalidDataException("Invalid battle opening");
            var session=new BattleSession(catalog,Deployment,campaignStage:Stage,useCT:CT,utilityAI:Utility,objective:Objective,legacyCampaign:Legacy,teamTurns:TeamTurns,phaseSurvival:PhaseSurvival,tacticalCombat:TacticalCombat,bossEncounters:BossEncounters,difficulty:Version>=2?Difficulty:BattleDifficulty.Standard,missionEvents:Version>=2&&MissionEvents,reactions:Version>=3&&Reactions);
            if(Units==null||Units.Count!=session.Units.Count)throw new InvalidDataException("Invalid opening roster");
            foreach(var tile in session.Grid.Tiles.Values)tile.Occupant=null;
            for(int i=0;i<Units.Count;i++)
            {
                var saved=Units[i];
                if(saved==null||saved.HP<=0||saved.Moved||saved.Acted||saved.Undo||saved.TurnsStarted!=0||saved.IntentPhase!=0)throw new InvalidDataException("Opening contains turn progress");
                saved.Apply(session.Units[i],session.Grid,catalog);
            }
            if(session.Result!=BattleResult.Ongoing)throw new InvalidDataException("Opening is already completed");
            session.Opening=this;return session;
        }
    }
}
