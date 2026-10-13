using System;
namespace TalesTactics
{
    // A victory snapshot is retained through failed saves. Never roll inside TryReward.
    public sealed class CampaignReward
    {
        public int Stage {get;}
        public int Gold {get;}
        public int Experience {get;}
        public string BonusEquipment {get;}
        public bool Repeat {get;}
        public bool Applied {get;internal set;}
        public string RouteFlag {get;}
        internal CampaignReward(int stage,bool repeat,int roll,int choice=-1)
        {Stage=stage;Repeat=repeat;Gold=CampaignEconomy.Gold(stage,repeat);Experience=CampaignEconomy.Experience(stage,repeat);BonusEquipment=CampaignEconomy.Drop(stage,roll);
            if(choice>=0&&(PersonalScenarios.Contains(stage)&&PersonalScenarios.Tier(stage)==1||stage==18))RouteFlag=CampaignStages.Id(stage)+".route."+(choice==0?"left":"right");}
    }
    public static class CampaignEconomy
    {
        public static int Gold(int stage,bool repeat)=>repeat?CampaignStages.Get(stage).RepeatGold:CampaignStages.Get(stage).FirstGold;
        public static int Experience(int stage,bool repeat)=>repeat?CampaignStages.Get(stage).RepeatEXP:CampaignStages.Get(stage).FirstEXP;
        public static string Drop(int stage,int roll)
        {
            if(stage<0||stage>=CampaignStages.Count)throw new ArgumentOutOfRangeException(nameof(stage));
            if(roll<0||roll>=10000)throw new ArgumentOutOfRangeException(nameof(roll));
            if(PersonalScenarios.Contains(stage))return null;
            return roll<2500?"bronze-sword":roll<4000?(stage==0?"leather-armor":"reinforced-armor"):null;
        }
        public static CampaignReward Prepare(CampaignSave save,int stage,int roll,int choice=-1)
        {
            if(!CampaignStages.Unlocked(save,stage))throw new ArgumentException("Chapter is locked");
            if(choice< -1||choice>1)throw new ArgumentOutOfRangeException(nameof(choice));
            return new CampaignReward(stage,save.StoryProgress.Contains(CampaignStages.Id(stage)),roll,choice);
        }
        public static string Preview(CampaignSave save,int stage)
        {
            bool repeat=save.StoryProgress.Contains(CampaignStages.Id(stage));
            if(PersonalScenarios.Contains(stage))return repeat?"완료한 이야기 · 보상 재지급 없음":"최초 보상: EXP "+Experience(stage,false)+" / "+Gold(stage,false)+"G\n"+(PersonalScenarios.Tier(stage)==2?"전용 무기: "+PersonalScenarios.Arc(stage).WeaponName:"이야기 진행 기록");
            return (repeat?"반복":"최초")+" 보상: 전원 EXP "+Experience(stage,repeat)+" / "+Gold(stage,repeat)+"G\n"+
                "확정: "+CampaignStages.Get(stage).EquipmentName+" 1개 · 추가: 청동검 25%, "+(stage==0?"가죽 갑옷":"강화 갑옷")+" 15%, 없음 60%";
        }
    }
}
