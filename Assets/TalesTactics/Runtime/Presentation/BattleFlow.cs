using System.Collections.Generic;
using System.Linq;
namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        readonly Dictionary<string,SkillData> lastSkills=new Dictionary<string,SkillData>();
        public SkillData LastSkill=>Session?.Active!=null&&lastSkills.TryGetValue(Session.Active.Data.Id,out var skill)?skill:null;
        public void RememberSkill(SkillData skill){if(Session?.Active!=null&&skill!=null)lastSkills[Session.Active.Data.Id]=skill;}
        public void CancelTarget()
        {
            if(SelectedSkill!=null&&SelectedSkill!=Session.Active.Data.BasicAttack)SetState(new ActionSelectionState(this));
            else SetState(new CommandState(this));
        }
        Dictionary<string,int> resultLevels,resultEXP,resultInventory;
        int resultGold;
        public string ResultRewards {get;private set;}="";
        public string[] ResultGrowth {get;private set;}=new string[0];
        public bool ResultTraining=>battleTraining;
        public bool CanPrepareNext=>Session?.Result==BattleResult.Victory&&!battleTraining&&!RewardPending&&pendingReward?.Applied==true&&battleStage+1<CampaignStages.Count;
        public string ResultStage=>battleTraining?"훈련 전투":CampaignStages.Title(battleStage);
        public void PrepareNextBattle(){if(!CanPrepareNext)return;int next=battleStage+1;Restart();SelectedStage=next;Hud.ShowChapterPage(next/3);}
        public bool CanRetryOpening=>Session?.Opening!=null&&!RewardPending&&!TutorialActive&&!StoryActive&&!TimingActive&&
            (IsPlayerCommand&&Session.Result==BattleResult.Ongoing||State is BattleEndState&&Session.Result==BattleResult.Defeat);
        public bool RetryOpening()
        {
            if(!CanRetryOpening)return false;
            BattleSession restored;
            try{restored=Session.Opening.Restore(Catalog);}catch(System.Exception){SaveNotice="전투 시작 기록을 복원할 수 없습니다. 현재 전투를 유지했습니다.";return false;}
            int stage=battleStage;Restart();Session=restored;battleStage=stage;battleTraining=restored.CampaignStage<0;TrainingMode=battleTraining;SelectedStage=stage;
            completed=false;pendingReward=null;TrainingObjective=restored.Objective;Deployment.Clear();Deployment.AddRange(restored.Opening.Deployment);
            UnityEngine.Random.state=restored.Opening.RandomState;Board.Build(Session);Audio.PlayBattle(!battleTraining&&CampaignStages.Get(stage).BossMusic);
            Message="출전 당시 편성·장비·규칙으로 재도전합니다.";SetState(new TurnStartState(this));return true;
        }
        public void RetryBattle(){if(RewardPending||!(State is BattleEndState))return;if(Session.Result==BattleResult.Defeat){RetryOpening();return;}Restart();BeginBattle();}
        void BeginResultSummary()
        {
            ResultGrowth=new string[0];ResultRewards="획득 보상 없음 · 저장된 성장과 장비는 유지됩니다.";
            if(battleTraining){ResultRewards="훈련 · 경험치·골드·장비·장 완료 보상은 저장하지 않습니다.";return;}
            if(Session.Result!=BattleResult.Victory)return;
            var party=Session.Units.Where(u=>u.Team==Team.Player).Select(u=>Campaign.Get(u.Data.Id)).ToArray();
            resultLevels=party.ToDictionary(p=>p.Id,p=>p.Level);resultEXP=party.ToDictionary(p=>p.Id,p=>p.EXP);
            resultInventory=Campaign.Inventory.ToDictionary(e=>e.Id,e=>e.Count);resultGold=Campaign.Gold;
        }
        void UpdateResultSummary(bool saved)
        {
            if(!saved){ResultRewards="보상 저장 실패 · 경험치·골드·장비가 아직 적용되지 않았습니다.\n재시도해도 같은 추첨 결과를 사용합니다.";ResultGrowth=new string[0];return;}
            var items=Campaign.Inventory.Where(e=>e.Count>(resultInventory.TryGetValue(e.Id,out var count)?count:0)).Select(e=>(Catalog.Equipment.FirstOrDefault(x=>x.Id==e.Id)?.DisplayName??e.Id)+" +"+(e.Count-(resultInventory.TryGetValue(e.Id,out var count)?count:0)));
            ResultRewards="각 출전 캐릭터 EXP +"+pendingReward.Experience+" · 골드 +"+(Campaign.Gold-resultGold)+" G (소지 "+Campaign.Gold+" G)\n획득 장비: "+(items.Any()?string.Join(" · ",items):"없음 · 보유 상한 확인");
            ResultGrowth=Session.Units.Where(u=>u.Team==Team.Player).Select(u=>
            {
                var p=Campaign.Get(u.Data.Id);int unlocked=u.Data.Skills.Count(s=>s.UnlockLevel>resultLevels[p.Id]&&s.UnlockLevel<=p.Level);
                return u.Data.DisplayName+"    Lv"+resultLevels[p.Id]+" → "+p.Level+"    EXP "+resultEXP[p.Id]+" → "+p.EXP+(p.Level>=50?" (최고 레벨)":" / "+p.Level*100)+(unlocked>0?"    새 기술 "+unlocked+"개":"");
            }).ToArray();
        }
    }
}
