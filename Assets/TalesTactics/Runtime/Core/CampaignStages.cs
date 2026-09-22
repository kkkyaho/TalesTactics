using System;
using System.Linq;
namespace TalesTactics
{
    // Prototype scenarios share TestStage; IDs are persisted story completion flags.
    public static class CampaignStages
    {
        public const int Count=2;
        public static int GoldReward(int stage)=>stage==0?120:180;
        public static string EquipmentReward(int stage)=>stage==0?"vital-charm":stage==1?"iron-sword":null;
        public static string Id(int stage)=>"chapter"+(stage+1);
        public static string Title(int stage)=>stage==0?"1장 · 유적의 경계":"2장 · 유적의 수호자";
        public static bool Unlocked(CampaignSave save,int stage)=>stage>=0&&stage<Count&&(stage==0||save.StoryProgress.Contains(Id(stage-1)));
        public static bool TryReward(CampaignSave save,int stage,CharacterData[] party,Func<CampaignSave,bool> persist)
        {
            if(!Unlocked(save,stage)||persist==null)return false;
            string rewardId=EquipmentReward(stage);
            var rewardEntry=save.Inventory.Find(e=>e.Id==rewardId);
            int originalCount=rewardEntry?.Count??0;
            bool rewardAdded=false;
            int originalGold=save.Gold;
            var originalCharacters=save.Characters.ToArray();
            var progress=party.Distinct().Select(c=>save.Get(c.Id)).ToArray();
            var levels=progress.Select(c=>c.Level).ToArray();var exp=progress.Select(c=>c.EXP).ToArray();
            var skills=progress.Select(c=>c.UnlockedSkills).ToArray();
            bool added=!save.StoryProgress.Contains(Id(stage)),saved=false;
            try
            {
                foreach(var character in party.Distinct())
                {var p=save.Get(character.Id);p.AddExperience(120);p.UnlockedSkills=character.Skills.Where(s=>s.UnlockLevel<=p.Level).Select(s=>s.Id).ToList();}
                save.Gold=(int)Math.Min(CampaignInventory.MaxGold,(long)save.Gold+GoldReward(stage));
                if(originalCount<CampaignInventory.MaxQuantity)
                {
                    if(rewardEntry==null){rewardEntry=new OwnedEquipment{Id=rewardId};save.Inventory.Add(rewardEntry);rewardAdded=true;}
                    rewardEntry.Count++;
                }
                if(added)save.StoryProgress.Add(Id(stage));
                return saved=persist(save);
            }
            finally
            {
                if(!saved)
                {
                    for(int i=0;i<progress.Length;i++){progress[i].Level=levels[i];progress[i].EXP=exp[i];progress[i].UnlockedSkills=skills[i];}
                    if(rewardAdded)save.Inventory.Remove(rewardEntry);else if(rewardEntry!=null)rewardEntry.Count=originalCount;
                    save.Gold=originalGold;save.Characters.Clear();save.Characters.AddRange(originalCharacters);
                    if(added)save.StoryProgress.Remove(Id(stage));
                }
            }
        }
    }
}
