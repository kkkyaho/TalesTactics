using System;
using System.Linq;
namespace TalesTactics
{
    // IDs are persisted story completion flags. Rewards are committed atomically.
    public static class CampaignStages
    {
        public const int Count=3;
        public static int GoldReward(int stage)=>CampaignEconomy.Gold(stage,false);
        public static string EquipmentReward(int stage)=>stage==0?"vital-charm":stage==1?"iron-sword":stage==2?"reinforced-armor":null;
        public static int EnemyLevel(int stage)=>stage==0?1:stage==1?3:stage==2?4:throw new ArgumentOutOfRangeException(nameof(stage));
        public static string Id(int stage)=>"chapter"+(stage+1);
        public static string Title(int stage)=>stage==0?"1장 · 유적의 경계":stage==1?"2장 · 유적의 수호자":"3장 · 협곡의 봉화";
        public static bool Unlocked(CampaignSave save,int stage)=>stage>=0&&stage<Count&&(stage==0||save.StoryProgress.Contains(Id(stage-1)));
        public static bool TryReward(CampaignSave save,int stage,CharacterData[] party,Func<CampaignSave,bool> persist)
            =>!Unlocked(save,stage)?false:TryReward(save,CampaignEconomy.Prepare(save,stage,9999),party,persist);
        public static bool TryReward(CampaignSave save,CampaignReward reward,CharacterData[] party,Func<CampaignSave,bool> persist)
        {
            if(reward==null||reward.Applied||!Unlocked(save,reward.Stage)||persist==null)return false;
            int stage=reward.Stage;
            if(save.StoryProgress.Contains(Id(stage))!=reward.Repeat)return false;
            var originalInventory=save.Inventory.ToArray();var counts=originalInventory.Select(e=>e.Count).ToArray();
            int originalGold=save.Gold;
            var originalCharacters=save.Characters.ToArray();
            var progress=party.Distinct().Select(c=>save.Get(c.Id)).ToArray();
            var levels=progress.Select(c=>c.Level).ToArray();var exp=progress.Select(c=>c.EXP).ToArray();
            var skills=progress.Select(c=>c.UnlockedSkills).ToArray();
            bool added=!save.StoryProgress.Contains(Id(stage)),saved=false;
            try
            {
                foreach(var character in party.Distinct())
                {var p=save.Get(character.Id);p.AddExperience(reward.Experience);p.UnlockedSkills=character.Skills.Where(s=>s.UnlockLevel<=p.Level).Select(s=>s.Id).ToList();}
                save.Gold=(int)Math.Min(CampaignInventory.MaxGold,(long)save.Gold+reward.Gold);
                foreach(var rewardId in new[]{EquipmentReward(stage),reward.BonusEquipment}.Where(id=>id!=null))
                {
                    var entry=save.Inventory.Find(e=>e.Id==rewardId);
                    if(entry==null){entry=new OwnedEquipment{Id=rewardId};save.Inventory.Add(entry);}
                    if(entry.Count<CampaignInventory.MaxQuantity)entry.Count++;
                }
                if(added)save.StoryProgress.Add(Id(stage));
                saved=persist(save);if(saved)reward.Applied=true;return saved;
            }
            finally
            {
                if(!saved)
                {
                    for(int i=0;i<progress.Length;i++){progress[i].Level=levels[i];progress[i].EXP=exp[i];progress[i].UnlockedSkills=skills[i];}
                    for(int i=0;i<originalInventory.Length;i++)originalInventory[i].Count=counts[i];
                    save.Inventory.Clear();save.Inventory.AddRange(originalInventory);
                    save.Gold=originalGold;save.Characters.Clear();save.Characters.AddRange(originalCharacters);
                    if(added)save.StoryProgress.Remove(Id(stage));
                }
            }
        }
    }
}
