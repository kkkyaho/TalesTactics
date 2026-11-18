using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public static partial class CrossoverTacticalMissions
    {
        public static Vector2Int[] Signals(int stage,int openingRevision=1,int tacticsRevision=1)
            =>Applies(stage,tacticsRevision)?Get(stage).Signals:CrossoverOpeningMissions.Signals(stage,openingRevision);
        public static Vector2Int Exit(int stage,int openingRevision=1,int tacticsRevision=1)
            =>Applies(stage,tacticsRevision)?Get(stage).Exit:CrossoverOpeningMissions.Exit(stage,openingRevision);
        public static Vector2Int Contract(int stage,int tacticsRevision=1)
            =>Applies(stage,tacticsRevision)?Get(stage).Contract:CrossoverBattle.ContractSeal;
        public static Vector2Int[] EnemySpawns(int stage,int openingRevision=1,int tacticsRevision=1)
            =>Applies(stage,tacticsRevision)?Get(stage).EnemySpawns:
                Enumerable.Range(0,9).Select(i=>CrossoverOpeningMissions.Applies(stage,openingRevision)
                    ?CrossoverOpeningMissions.EnemyPosition(CrossoverCampaign.Episode(stage).Number,i):CrossoverCampaign.EnemySpawn(i)).ToArray();
        public static bool RequiresDevices(int stage,int tacticsRevision=1)
        {
            if(!Applies(stage,tacticsRevision))return false;
            if(PersonalScenarios.Contains(stage))return PersonalScenarios.Tier(stage)==2
                &&new[]{"jade","natalia","alphen"}.Contains(PersonalScenarios.Arc(stage).Owner);
            var goal=Get(stage).Goal;
            return goal==TacticalMissionGoal.DevicesBoss||goal==TacticalMissionGoal.DevicesSignalsExit||goal==TacticalMissionGoal.Finale;
        }
    }
    public sealed partial class CrossoverBattle
    {
        public bool RevisedTactics=>CrossoverTacticalMissions.Applies(battle.CampaignStage,State.TacticsRevision);
        public TacticalMissionLayout TacticalLayout=>RevisedTactics?CrossoverTacticalMissions.Get(battle.CampaignStage):null;
        public Vector2Int ContractPosition=>CrossoverTacticalMissions.Contract(battle.CampaignStage,State.TacticsRevision);
        public Vector2Int[] EnemySpawnPositions=>CrossoverTacticalMissions.EnemySpawns(battle.CampaignStage,State.OpeningRevision,State.TacticsRevision);
        public UnitRuntime[] ObjectiveDevices=>PrisonDevices.Length>0?PrisonDevices:
            CrossoverTacticalMissions.RequiresDevices(battle.CampaignStage,State.TacticsRevision)?Enemies.Where(u=>u.Data.Id=="boundary-device").ToArray():Array.Empty<UnitRuntime>();
        internal void SynchronizeResolver()
        {
            battle.Resolver.CrossoverState=State;
            battle.Resolver.CrossoverSignals=(Vector2Int[])SignalPositions.Clone();
            battle.Resolver.CrossoverEnemySpawns=(Vector2Int[])EnemySpawnPositions.Clone();
        }
        static bool TacticalUsesExit(TacticalMissionGoal goal)=>goal==TacticalMissionGoal.SignalsExit||goal==TacticalMissionGoal.DevicesSignalsExit;
        bool TacticalVictory
        {
            get
            {
                switch(TacticalLayout.Goal)
                {
                    case TacticalMissionGoal.Boss:return !Boss.Alive;
                    case TacticalMissionGoal.SignalsBoss:return AllSignalsConnected&&!Boss.Alive;
                    case TacticalMissionGoal.DevicesBoss:return DevicesDown&&!Boss.Alive;
                    case TacticalMissionGoal.SignalsExit:return AllSignalsConnected&&AtExit;
                    case TacticalMissionGoal.DevicesSignalsExit:return DevicesDown&&AllSignalsConnected&&AtExit;
                    case TacticalMissionGoal.SplitFronts:return State.Segment==1&&!Boss.Alive&&!Enemies[4].Alive;
                    case TacticalMissionGoal.Finale:return AllSignalsConnected&&DevicesDown&&!Boss.Alive&&Boss.BossPhase==3;
                    default:throw new InvalidOperationException("Personal objectives use their authored trial rules");
                }
            }
        }
    }
}
