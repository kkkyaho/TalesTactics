using System;
using System.Linq;
namespace TalesTactics
{
    // IDs are persisted story completion flags. Rewards are committed atomically.
    public static class CampaignStages
    {
        // Persisted stage indexes never change; append new chapters only.
        public sealed class Chapter
        {
            public readonly string Title, Equipment, EquipmentName;
            public readonly int EnemyLevel, EntryLevel, FirstEXP, RepeatEXP, FirstGold, RepeatGold;
            public readonly bool BossMusic;
            public Chapter(string title,int enemy,int entry,int exp,int gold,string equipment,string equipmentName,bool boss=false)
            {Title=title;EnemyLevel=enemy;EntryLevel=entry;FirstEXP=exp;RepeatEXP=10;FirstGold=gold;RepeatGold=gold/2;Equipment=equipment;EquipmentName=equipmentName;BossMusic=boss;}
        }
        static readonly Chapter[] chapters={
            new Chapter("1장 · 유적의 경계",1,1,30,120,"vital-charm","생명의 부적"),
            new Chapter("2장 · 유적의 수호자",3,2,30,180,"iron-sword","철검",true),
            new Chapter("3장 · 협곡의 봉화",4,3,30,240,"reinforced-armor","강화 갑옷"),
            new Chapter("4장 · 별을 읽는 탑",5,4,30,320,"guardian-medal","수호의 메달"),
            new Chapter("5장 · 잠긴 수문",6,5,30,420,"tempered-armor","단련 갑옷"),
            new Chapter("6장 · 새벽의 중계핵",8,7,30,560,"guardian-medal","수호의 메달",true)
        };
        public const int LegacyCount=6;
        public static int Count=>PersonalScenarios.FirstStage+PersonalScenarios.Count;
        public static Chapter Get(int stage)=>CrossoverCampaign.IsMain(stage)?CrossoverCampaign.Chapter(stage):PersonalScenarios.Contains(stage)?PersonalScenarios.Chapter(stage):stage>=0&&stage<LegacyCount?chapters[stage]:throw new ArgumentOutOfRangeException(nameof(stage));
        public static int GoldReward(int stage)=>CampaignEconomy.Gold(stage,false);
        public static string EquipmentReward(int stage)=>Get(stage).Equipment;
        public static int EnemyLevel(int stage)=>Get(stage).EnemyLevel;
        public static string Id(int stage)=>CrossoverCampaign.IsMain(stage)?CrossoverCampaign.Flag(CrossoverCampaign.Episode(stage).Number):PersonalScenarios.Contains(stage)?PersonalScenarios.Flag(stage):"chapter"+(stage+1);
        public static string Title(int stage)=>Get(stage).Title.Replace("화 · ","화 ");
        public static bool Unlocked(CampaignSave save,int stage)=>PersonalScenarios.Contains(stage)?PersonalScenarios.Unlocked(save,stage):stage>=0&&stage<Count&&(stage==0||stage==CrossoverCampaign.FirstStage||save.StoryProgress.Contains(Id(stage-1)));
        public static bool HasNext(int stage)=>stage>=0&&stage<LegacyCount-1||CrossoverCampaign.IsMain(stage)&&stage<CrossoverCampaign.LastStage;
        public static bool TryReward(CampaignSave save,int stage,CharacterData[] party,Func<CampaignSave,bool> persist)
            =>!Unlocked(save,stage)?false:TryReward(save,CampaignEconomy.Prepare(save,stage,9999),party,persist);
        public static bool TryReward(CampaignSave save,CampaignReward reward,CharacterData[] party,Func<CampaignSave,bool> persist,System.Collections.Generic.IReadOnlyDictionary<string,int> actionExperience=null)
        {
            if(reward==null||reward.Applied||!Unlocked(save,reward.Stage)||persist==null)return false;
            int stage=reward.Stage;
            if(PersonalScenarios.Contains(stage)&&save.StoryProgress.Contains(Id(stage)))return false;
            if(save.StoryProgress.Contains(Id(stage))!=reward.Repeat)return false;
            var originalInventory=save.Inventory.ToArray();var counts=originalInventory.Select(e=>e.Count).ToArray();
            int originalGold=save.Gold;
            var originalCharacters=save.Characters.ToArray();
            var progress=party.Distinct().Select(c=>save.Get(c.Id)).ToArray();
            var levels=progress.Select(c=>c.Level).ToArray();var exp=progress.Select(c=>c.EXP).ToArray();
            var skills=progress.Select(c=>c.UnlockedSkills).ToArray();
            bool added=!save.StoryProgress.Contains(Id(stage)),saved=false;
            bool addedRoute=added&&!string.IsNullOrEmpty(reward.RouteFlag)&&!save.StoryProgress.Contains(reward.RouteFlag);
            try
            {
                foreach(var character in party.Distinct())
                {var p=save.Get(character.Id);p.AddExperience(reward.Experience+(actionExperience!=null&&actionExperience.TryGetValue(character.Id,out int earned)?earned:0));p.UnlockedSkills=character.Skills.Where(s=>s.UnlockLevel<=p.Level).Select(s=>s.Id).ToList();}
                save.Gold=(int)Math.Min(CampaignInventory.MaxGold,(long)save.Gold+reward.Gold);
                foreach(var rewardId in new[]{EquipmentReward(stage),reward.BonusEquipment}.Where(id=>id!=null))
                {
                    var entry=save.Inventory.Find(e=>e.Id==rewardId);
                    if(entry==null){entry=new OwnedEquipment{Id=rewardId};save.Inventory.Add(entry);}
                    if(entry.Count<CampaignInventory.MaxQuantity)entry.Count++;
                }
                if(added)save.StoryProgress.Add(Id(stage));
                if(addedRoute)save.StoryProgress.Add(reward.RouteFlag);
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
                    if(addedRoute)save.StoryProgress.Remove(reward.RouteFlag);
                }
            }
        }
    }
}
