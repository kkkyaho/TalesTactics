using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    [Serializable] public sealed class BattleCheckpoint
    {
        public int Version=6,Stage,Active,Round,SurvivalTurns;
        public bool LegacyMission;
        public bool HasOpening;
        public BattleOpening Opening;
        public bool CT,Utility,TeamTurns,PhaseSurvival,TacticalCombat,BossEncounters;
        public int[] Deployment,Order,Begun;
        public long[] Charges;
        public UnityEngine.Random.State RandomState;
        public List<CheckpointUnit> Units=new List<CheckpointUnit>();
        public static BattleCheckpoint Capture(BattleSession session,int[] deployment)
        {
            if(session.CampaignStage<0||session.Result!=BattleResult.Ongoing||session.Active?.Team!=Team.Player)throw new InvalidOperationException("Only an ongoing campaign player command can be suspended");
            var save=new BattleCheckpoint{Opening=session.Opening,HasOpening=session.Opening!=null,Stage=session.CampaignStage,Deployment=deployment.ToArray(),Active=session.Units.IndexOf(session.Active),CT=session.Scheduler is CTTurnScheduler,Utility=session.UseUtilityAI,RandomState=UnityEngine.Random.state,
                BossEncounters=session.BossEncounters,TacticalCombat=session.TacticalCombat,PhaseSurvival=(session.Victory as SurviveTurns)?.EnemyPhases??false,LegacyMission=session.LegacyCampaign,SurvivalTurns=(session.Victory as SurviveTurns)?.Completed??0};
            if(session.Scheduler is SpeedTurnScheduler speed){save.Order=speed.Capture(session.Units);save.Round=speed.Round;}
            else if(session.Scheduler is TeamTurnScheduler team){save.TeamTurns=true;save.Order=team.CapturePending(session.Units);save.Begun=team.CaptureBegun(session.Units);save.Round=team.Round;}
            else if(session.Scheduler is CTTurnScheduler ct)save.Charges=ct.Capture(session.Units);else throw new InvalidOperationException("Unsupported scheduler");
            foreach(var u in session.Units)save.Units.Add(new CheckpointUnit(u));
            return save;
        }
        public BattleSession Restore(BattleCatalog catalog)
        {
            if(Version<1||Version>6||TeamTurns&&(Version<3||CT)||Stage<0||Stage>=CampaignStages.Count||Deployment==null||Deployment.Length<1||Deployment.Length>catalog.Rules.MaxDeployment||Deployment.Distinct().Count()!=Deployment.Length||Deployment.Any(i=>i<0||i>=catalog.Characters.Length))throw new InvalidDataException("Unsupported checkpoint");
            var session=new BattleSession(catalog,Deployment,1,1,Stage,CT,Utility,legacyCampaign:Version==1||LegacyMission,teamTurns:TeamTurns,phaseSurvival:Version>=4&&PhaseSurvival,tacticalCombat:Version>=4&&TacticalCombat,bossEncounters:Version>=5&&BossEncounters);
            if(Units==null||Units.Count!=session.Units.Count||Active<0||Active>=Units.Count)throw new InvalidDataException("Invalid unit roster");
            foreach(var tile in session.Grid.Tiles.Values)tile.Occupant=null;
            for(int i=0;i<Units.Count;i++)Units[i].Apply(session.Units[i],session.Grid,catalog);
            if(session.Victory is SurviveTurns survival)survival.Restore(SurvivalTurns);
            else if(SurvivalTurns!=0)throw new InvalidDataException("Invalid mission progress");
            session.Active=session.Units[Active];
            if(!session.Active.Alive||session.Active.Team!=Team.Player||session.Result!=BattleResult.Ongoing)throw new InvalidDataException("Invalid active turn");
            if(CT){if(Charges==null||Charges.Length!=Units.Count||Charges.Any(v=>v<0||v>100000))throw new InvalidDataException("Invalid CT");((CTTurnScheduler)session.Scheduler).Restore(session.Units,Charges);}
            else if(TeamTurns)((TeamTurnScheduler)session.Scheduler).Restore(session.Units,Order,Begun,Round,session.Active);
            else{if(Order==null||Order.Any(i=>i<0||i>=Units.Count)||Order.Distinct().Count()!=Order.Length||Round<1)throw new InvalidDataException("Invalid turn queue");((SpeedTurnScheduler)session.Scheduler).Restore(session.Units,Order,Round);}
            if(Version>=6&&HasOpening)
            {
                if(Opening==null||Opening.Stage!=Stage||Opening.Deployment==null||!Opening.Deployment.SequenceEqual(Deployment))throw new InvalidDataException("Opening does not match checkpoint");
                Opening.Restore(catalog);session.Opening=Opening;
            }
            // Caller restores RNG only after the whole checkpoint has been validated.
            return session;
        }
    }
    [Serializable] public sealed class CheckpointUnit
    {
        public string Id;public int Level,HP,MP,Gauge,Claw;
        public bool Promoted,Moved,Acted,Undo,Flaming,Ignition;
        public TacticalTrait Trait;public GrowthPath Growth;
        public int Mastery,TurnsStarted,IntentTurn,IntentPhase;
        public bool Trial,TrialUsed,ProtectionUsed,TacticalEnemy;
        public string IntentSkill;public Vector2Int IntentAim;
        public Vector2Int Position,Origin;public Facing Facing,OriginalFacing;
        public string[] Equipment,CooldownIds;public int[] CooldownTurns;
        public List<RuntimeStatus> Statuses;
        public CheckpointUnit(UnitRuntime u)
        {
            Id=u.Data.Id;Level=u.Level;HP=u.CurrentHP;MP=u.CurrentMP;Gauge=u.SpecialGauge;Claw=u.ClawAttacks;Promoted=u.Promoted;
            Moved=u.Moved;Acted=u.Acted;Undo=u.CanUndoMove;Flaming=u.FlamingChain;Ignition=u.GuardIgnition;
            Growth=u.Growth;Trait=u.Trait;Mastery=u.CampaignMastery;Trial=u.UltimateTrial;TrialUsed=u.UltimateTrialUsed;ProtectionUsed=u.ProtectionUsed;
            TacticalEnemy=u.TacticalEnemy;TurnsStarted=u.TurnsStarted;IntentTurn=u.IntentTurn;IntentPhase=u.IntentPhase;IntentSkill=u.IntentSkill;IntentAim=u.IntentAim;
            Position=u.Position;Origin=u.MoveOrigin;Facing=u.Facing;OriginalFacing=u.OriginalFacing;
            Equipment=u.Equipment.Select(e=>e==null?null:e.Id).ToArray();CooldownIds=u.Cooldowns.Keys.ToArray();CooldownTurns=CooldownIds.Select(k=>u.Cooldowns[k]).ToArray();
            Statuses=u.Statuses.Select(s=>new RuntimeStatus{Kind=s.Kind,Turns=s.Turns,Fresh=s.Fresh}).ToList();
        }
        public void Apply(UnitRuntime u,GridMap grid,BattleCatalog catalog)
        {
            if(Id!=u.Data.Id||Level<1||Level>50||Equipment==null||Equipment.Length!=3||Statuses==null||CooldownIds==null||CooldownTurns==null||CooldownIds.Length!=CooldownTurns.Length||CooldownIds.Distinct().Count()!=CooldownIds.Length||!Enum.IsDefined(typeof(Facing),Facing)||!Enum.IsDefined(typeof(Facing),OriginalFacing))throw new InvalidDataException("Invalid unit data");
            if(!Enum.IsDefined(typeof(GrowthPath),Growth)||!Enum.IsDefined(typeof(TacticalTrait),Trait)||Mastery<0||Mastery>3||TurnsStarted<0||IntentTurn<0||IntentTurn>TurnsStarted||IntentPhase<0||IntentPhase>2)throw new InvalidDataException("Invalid tactics");
            u.Level=Level;u.Promoted=Promoted;u.Growth=Growth;u.Trait=Trait;u.CampaignMastery=Mastery;u.UltimateTrial=Trial;u.UltimateTrialUsed=TrialUsed;u.ProtectionUsed=ProtectionUsed;
            u.TacticalEnemy=TacticalEnemy;u.TurnsStarted=TurnsStarted;u.IntentTurn=IntentTurn;u.IntentPhase=IntentPhase;u.IntentSkill=IntentSkill;u.IntentAim=IntentAim;
            if(IntentPhase>0&&(grid[IntentAim]==null||EnemyTactics.Intent(u)==null))throw new InvalidDataException("Invalid telegraph");
            for(int i=0;i<3;i++){u.Equipment[i]=string.IsNullOrEmpty(Equipment[i])?null:catalog.Equipment.FirstOrDefault(e=>e.Id==Equipment[i]);if(!string.IsNullOrEmpty(Equipment[i])&&u.Equipment[i]==null)throw new InvalidDataException("Unknown equipment");}
            if(HP<0||HP>u.Stats.HP||MP<0||MP>u.Stats.MP||Gauge<0||Gauge>100||Claw<0||grid[Position]==null||!grid[Position].Walkable||Undo&&(grid[Origin]==null||!grid[Origin].Walkable))throw new InvalidDataException("Invalid unit state");
            u.CurrentHP=HP;u.CurrentMP=MP;u.SpecialGauge=Gauge;u.ClawAttacks=Claw;u.Moved=Moved;u.Acted=Acted;u.CanUndoMove=Undo;u.FlamingChain=Flaming;u.GuardIgnition=Ignition;u.Facing=Facing;u.OriginalFacing=OriginalFacing;u.MoveOrigin=Origin;u.Position=Position;
            if(u.Alive){if(grid[Position].Occupant!=null)throw new InvalidDataException("Overlapping units");grid[Position].Occupant=u;}
            foreach(var status in Statuses){if(status==null||status.Turns<1||!Enum.IsDefined(typeof(StatusKind),status.Kind))throw new InvalidDataException("Invalid status");u.Statuses.Add(new RuntimeStatus{Kind=status.Kind,Turns=status.Turns,Fresh=status.Fresh});}
            for(int i=0;i<CooldownIds.Length;i++){if(string.IsNullOrEmpty(CooldownIds[i])||CooldownTurns[i]<1)throw new InvalidDataException("Invalid cooldown");u.Cooldowns.Add(CooldownIds[i],CooldownTurns[i]);}
        }
    }
}
