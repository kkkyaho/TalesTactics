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
        internal CampaignReward(int stage,bool repeat,int roll)
        {Stage=stage;Repeat=repeat;Gold=CampaignEconomy.Gold(stage,repeat);Experience=CampaignEconomy.Experience(stage,repeat);BonusEquipment=CampaignEconomy.Drop(stage,roll);}
    }
    public static class CampaignEconomy
    {
        public static int Gold(int stage,bool repeat)=>stage==0?(repeat?60:120):stage==1?(repeat?90:180):stage==2?(repeat?120:240):0;
        public static int Experience(int stage,bool repeat)=>stage==0?(repeat?60:120):stage==1?(repeat?90:180):stage==2?(repeat?150:300):0;
        public static string Drop(int stage,int roll)
        {
            if(stage<0||stage>=CampaignStages.Count)throw new ArgumentOutOfRangeException(nameof(stage));
            if(roll<0||roll>=10000)throw new ArgumentOutOfRangeException(nameof(roll));
            return roll<2500?"bronze-sword":roll<4000?(stage==0?"leather-armor":"reinforced-armor"):null;
        }
        public static CampaignReward Prepare(CampaignSave save,int stage,int roll)
        {
            if(!CampaignStages.Unlocked(save,stage))throw new ArgumentException("Chapter is locked");
            return new CampaignReward(stage,save.StoryProgress.Contains(CampaignStages.Id(stage)),roll);
        }
        public static string Preview(CampaignSave save,int stage)
        {
            bool repeat=save.StoryProgress.Contains(CampaignStages.Id(stage));
            return (repeat?"반복":"최초")+" 보상: 전원 EXP "+Experience(stage,repeat)+" / "+Gold(stage,repeat)+"G\n"+
                "확정: "+(stage==0?"생명의 부적":stage==1?"철검":"강화 갑옷")+" 1개 · 추가: 청동검 25%, "+(stage==0?"가죽 갑옷":"강화 갑옷")+" 15%, 없음 60%";
        }
    }
}
