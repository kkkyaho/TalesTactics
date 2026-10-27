using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        int PreparationTrainingTarget=>battle.SelectedStage>=0&&battle.SelectedStage<CampaignStages.Count?CampaignStages.Get(battle.SelectedStage).EntryLevel:1;
        bool PreparationTrainingAvailable(CharacterData character)=>!battle.TrainingMode&&battle.IsJoined(character)&&
            CampaignStages.Unlocked(battle.Campaign,battle.SelectedStage)&&FormationProgress(character).Level<PreparationTrainingTarget;
        CharacterData[] PreparationTrainees()=>JoinedCharacters().Where(PreparationTrainingAvailable)
            .OrderByDescending(c=>battle.Deployment.Contains(Array.IndexOf(battle.Catalog.Characters,c))).ToArray();
        // This describes the current episode, not an unread notification. No new save flag
        // or speculative future companion is needed, and the label disappears next episode.
        CharacterData[] CurrentEpisodeArrivals()
        {
            if(battle.TrainingMode||!battle.Catalog.CrossoverReady||!CrossoverCampaign.IsMain(battle.SelectedStage))return Array.Empty<CharacterData>();
            int episode=CrossoverCampaign.Episode(battle.SelectedStage).Number;
            if(episode<=1||battle.Campaign.StoryProgress.Contains(CrossoverCampaign.Flag(episode)))return Array.Empty<CharacterData>();
            return JoinedCharacters().Where(c=>CrossoverCampaign.Joined(c.Id,episode)&&!CrossoverCampaign.Joined(c.Id,episode-1)).ToArray();
        }
        void ShowAvailableCompanions()
        {
            if(battle.Session!=null||InputModalOpen)return;
            formationFilter=0;resetFormationScroll=true;ShowDeployment();
        }
        void ShowPreparationGrowth()
        {
            if(battle.Session!=null||InputModalOpen)return;
            var candidate=PreparationTrainees().FirstOrDefault();
            if(candidate==null)ShowGrowthRoster();else ShowGrowth(candidate);
        }
        string PreparationGrowthNotice(CharacterData character)
        {
            if(!PreparationTrainingAvailable(character))return "저장된 캐릭터의 성장 정보를 표시합니다.";
            return "무료 합류 훈련: Lv"+FormationProgress(character).Level+" → Lv"+PreparationTrainingTarget+" · 골드 소모 없음.\n기술도 함께 해금되며 저장 후 다음 전투에 적용됩니다.";
        }
    }
}
