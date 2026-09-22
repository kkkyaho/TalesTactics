using System;
using System.Collections.Generic;
namespace TalesTactics
{
    public static class CampaignPromotion
    {
        public static string Unavailable(CharacterProgress progress,CharacterData data,List<string> flags)
        {
            if(progress.Promoted)return "이미 승급한 캐릭터입니다.";
            if(string.IsNullOrWhiteSpace(data.PromotionJob)||string.IsNullOrWhiteSpace(data.PromotionStoryFlag))return "승급 데이터가 아직 준비되지 않았습니다.";
            if(progress.Level<data.PromotionLevel)return "승급 레벨이 부족합니다.";
            if(flags==null||!flags.Contains(data.PromotionStoryFlag))return "스토리 조건을 달성하지 않았습니다.";
            return null;
        }
        public static bool TrySave(CampaignSave campaign,CharacterData data,Func<CampaignSave,bool> persist)
        {
            var progress=campaign.Get(data.Id);
            if(Unavailable(progress,data,campaign.StoryProgress)!=null)return false;
            bool saved=false;progress.Promoted=true;
            try{return saved=persist(campaign);}
            finally{if(!saved)progress.Promoted=false;}
        }
    }
}
