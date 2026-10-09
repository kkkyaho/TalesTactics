using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public enum BattleDifficulty { Standard, Relaxed, Veteran }
    public static class DifficultyRules
    {
        public static string Name(BattleDifficulty value)=>value==BattleDifficulty.Relaxed?"여유":value==BattleDifficulty.Veteran?"도전":"표준";
        public static int EnemyLevel(int level,BattleDifficulty value)=>Mathf.Clamp(level+(value==BattleDifficulty.Relaxed?-2:value==BattleDifficulty.Veteran?2:0),1,50);
    }
    public sealed class CaptureDestination : IVictoryCondition
    {
        readonly BattleSession battle;
        public CaptureDestination(BattleSession battle){this.battle=battle;}
        public BattleResult Evaluate(System.Collections.Generic.IReadOnlyList<UnitRuntime> units)=>!units.Any(u=>u.Alive&&u.Team==Team.Player)?BattleResult.Defeat:battle.CaptureProgress>=2?BattleResult.Victory:BattleResult.Ongoing;
    }
    public sealed partial class BattleSession
    {
        public readonly BattleDifficulty Difficulty;
        public readonly bool MissionEvents;
        readonly BattleCatalog catalog;
        public bool ReinforcementsArrived {get;private set;}
        public int CaptureProgress {get;private set;}
        public bool CaptureMission=>MissionEvents&&CampaignStage==2;
        public bool ReinforcementMission=>MissionEvents&&(CampaignStage==2||CampaignStage==4);
        public string EventSummary=>!ReinforcementMission?"":ReinforcementsArrived?"증원 2명 도착":"3턴 시작 · 적 증원 2명";
        void AdvanceMissionEvents()
        {
            if(ReinforcementMission&&!ReinforcementsArrived&&Scheduler is TeamTurnScheduler team&&team.Round>=3&&team.Phase==Team.Player)SpawnReinforcements();
        }
        void CompleteMissionPhase(TeamTurnScheduler team)
        {
            if(!CaptureMission||team.Phase!=Team.Player||team.Preview(Units).Count!=0)return;
            var holder=Grid[Destination]?.Occupant;
            CaptureProgress=holder?.Team==Team.Player&&holder.Alive?CaptureProgress+1:0;
        }
        bool SpawnReinforcements()
        {
            if(!ReinforcementMission||ReinforcementsArrived)return false;
            var origin=CampaignContent.EnemySpawn(CampaignStage,3);
            var positions=Grid.Tiles.Values.Where(t=>t.Walkable&&t.Occupant==null&&t.Coordinate!=Destination)
                .OrderBy(t=>GridMap.Distance(t.Coordinate,origin)).ThenBy(t=>t.Coordinate.y).ThenBy(t=>t.Coordinate.x).Take(2).ToArray();
            if(positions.Length<2)return false;
            int level=Units.First(u=>u.Team==Team.Enemy).Level;
            for(int i=0;i<2;i++)
            {
                var u=new UnitRuntime(CampaignEnemies.Resolve(catalog,CampaignStage,i+2),Team.Enemy,Rules,level);Units.Add(u);Grid.Place(u,positions[i].Coordinate);EnemyTactics.ConfigureUnit(u,catalog);
            }
            ReinforcementsArrived=true;return true;
        }
        public void RestoreMissionEvents(bool arrived,int progress)
        {
            if(progress<0||progress>1||!CaptureMission&&progress!=0||arrived&&!ReinforcementMission)throw new InvalidDataException("Invalid mission event progress");
            if(arrived&&!SpawnReinforcements())throw new InvalidDataException("Cannot restore reinforcements");
            CaptureProgress=progress;
        }
    }
}
