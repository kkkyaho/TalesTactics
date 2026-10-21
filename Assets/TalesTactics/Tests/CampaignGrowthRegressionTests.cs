using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void CampaignGrowthMainCompletionUnlocksCorrespondingLegacyShopRequirement(int episode)
        {
            var item=New<EquipmentData>();item.Id="progression-gear";item.BuyPrice=100;item.RequiredStoryFlag="chapter"+episode;
            var save=new CampaignSave();
            for(int n=1;n<episode;n++)save.StoryProgress.Add(CrossoverCampaign.Flag(n));
            Assert.That(CampaignInventory.PurchaseUnlockRequirement(save,item),Is.EqualTo("해금 조건: 메인 "+episode+"화 완료"));
            Assert.That(CampaignInventory.TryBuy(save,item,_=>true),Is.False);
            save.StoryProgress.Add(CrossoverCampaign.Flag(episode));string before=JsonUtility.ToJson(save);
            Assert.That(CampaignInventory.PurchaseUnavailable(save,item),Is.Null);
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before),"Reading availability must not manufacture old completion flags");
            Assert.That(CampaignInventory.TryBuy(save,item,_=>true),Is.True);
            Assert.That(save.Gold,Is.EqualTo(200));Assert.That(CampaignInventory.Owned(save,item.Id),Is.EqualTo(1));
            Assert.That(save.StoryProgress.Any(f=>f.StartsWith("chapter")),Is.False);
            save=new CampaignSave();save.StoryProgress.Add(item.RequiredStoryFlag);
            Assert.That(CampaignInventory.PurchaseUnlockRequirement(save,item),Is.Null,"Old saves retain their existing unlock");
        }

        [TestCase(1)] [TestCase(6)] [TestCase(20)]
        public void CampaignGrowthDirectMainShopRequirementUsesEpisodeNumber(int episode)
        {
            var item=New<EquipmentData>();item.RequiredStoryFlag=CrossoverCampaign.Flag(episode);
            var save=new CampaignSave();save.StoryProgress.Add("chapter"+episode);
            Assert.That(CampaignInventory.PurchaseUnlockRequirement(save,item),Is.EqualTo("해금 조건: 메인 "+episode+"화 완료"));
            save.StoryProgress.Add(CrossoverCampaign.Flag(episode));
            Assert.That(CampaignInventory.PurchaseUnlockRequirement(save,item),Is.Null);
        }

        [Test] public void CampaignGrowthShopCustomConditionsStayExactAndPurchaseFailureRollsBack()
        {
            var item=New<EquipmentData>();item.Id="progression-gear";item.BuyPrice=100;item.RequiredStoryFlag="custom.guild";
            var save=new CampaignSave();for(int n=1;n<=20;n++)save.StoryProgress.Add(CrossoverCampaign.Flag(n));
            Assert.That(CampaignInventory.PurchaseUnlockRequirement(save,item),Is.EqualTo("해금 조건: custom.guild"));
            item.RequiredStoryFlag="chapter7";
            Assert.That(CampaignInventory.PurchaseUnlockRequirement(save,item),Is.EqualTo("해금 조건: chapter7"),"Only the retired six chapter conditions have aliases");
            item.RequiredStoryFlag="chapter1";string before=JsonUtility.ToJson(save);
            Assert.That(CampaignInventory.TryBuy(save,item,_=>false),Is.False);
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            Assert.Throws<IOException>(()=>CampaignInventory.TryBuy(save,item,_=>throw new IOException("test save failure")));
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
        }

        [TestCase(false)] [TestCase(true)]
        public void CampaignGrowthNewRecruitTrainingFailureDoesNotCreateProgress(bool throws)
        {
            var save=new CampaignSave();for(int n=1;n<5;n++)save.StoryProgress.Add(CrossoverCampaign.Flag(n));
            data.Id="kisara";data.Skills=Array.Empty<SkillData>();string before=JsonUtility.ToJson(save);int writes=0,attemptedLevel=0;
            bool Persist(CampaignSave current)
            {
                writes++;attemptedLevel=current.Characters.Single().Level;
                if(throws)throw new IOException("test save failure");return false;
            }
            Assert.That(TacticalDevelopment.CatchUp(save,data,CrossoverCampaign.FirstStage+4,Persist),Is.False);
            Assert.That(writes,Is.EqualTo(1));Assert.That(attemptedLevel,Is.EqualTo(7));
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
        }

        [Test] public void CampaignGrowthTrainingNoOpDoesNotCreateProgressOrWrite()
        {
            var save=new CampaignSave();data.Skills=Array.Empty<SkillData>();string before=JsonUtility.ToJson(save);int writes=0;
            bool Persist(CampaignSave current){writes++;return true;}
            Assert.That(TacticalDevelopment.CatchUp(save,data,CrossoverCampaign.FirstStage,Persist),Is.False,"Already at the level-one entry target");
            Assert.That(TacticalDevelopment.CatchUp(save,data,CrossoverCampaign.FirstStage+4,Persist),Is.False,"Locked episode");
            Assert.That(TacticalDevelopment.CatchUp(save,data,CrossoverCampaign.FirstStage,null),Is.False);
            Assert.That(writes,Is.Zero);Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
        }

        [TestCase(false)] [TestCase(true)]
        public void CampaignGrowthExistingTrainingFailurePreservesProgressAndReferences(bool throws)
        {
            var save=new CampaignSave();for(int n=1;n<5;n++)save.StoryProgress.Add(CrossoverCampaign.Flag(n));
            var progress=save.Get(data.Id);progress.Level=3;progress.EXP=55;progress.Promoted=true;
            progress.Trait=TacticalTrait.Protector;progress.UnlockedSkills.Add("existing-skill");
            var skill=Skill(EffectKind.Damage);skill.UnlockLevel=5;data.Skills=new[]{skill};
            var skills=progress.UnlockedSkills;var characters=save.Characters;string before=JsonUtility.ToJson(save);
            bool Persist(CampaignSave current){if(throws)throw new IOException("test save failure");return false;}
            Assert.That(TacticalDevelopment.CatchUp(save,data,CrossoverCampaign.FirstStage+4,Persist),Is.False);
            Assert.That(save.Characters,Is.SameAs(characters));Assert.That(save.Characters.Single(),Is.SameAs(progress));
            Assert.That(progress.UnlockedSkills,Is.SameAs(skills));Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
        }

        [Test] public void CampaignGrowthSuccessfulRecruitTrainingPersistsOnceAndKeepsHigherProgress()
        {
            var save=new CampaignSave();for(int n=1;n<5;n++)save.StoryProgress.Add(CrossoverCampaign.Flag(n));
            var unlocked=Skill(EffectKind.Damage);unlocked.Id="available";unlocked.UnlockLevel=7;
            var locked=Skill(EffectKind.Damage);locked.Id="later";locked.UnlockLevel=8;data.Skills=new[]{unlocked,locked};
            var file=new CampaignFile(SaveTestPath());int writes=0;
            Assert.That(TacticalDevelopment.CatchUp(save,data,CrossoverCampaign.FirstStage+4,current=>{writes++;return file.Save(current);}),Is.True);
            var read=file.Load();Assert.That(read.Characters.Count,Is.EqualTo(1));var progress=read.Get(data.Id);
            Assert.That(progress.Level,Is.EqualTo(7));Assert.That(progress.EXP,Is.Zero);
            Assert.That(progress.UnlockedSkills,Is.EqualTo(new[]{"available"}));Assert.That(writes,Is.EqualTo(1));
            progress.Level=12;progress.EXP=99;string before=JsonUtility.ToJson(read);
            Assert.That(TacticalDevelopment.CatchUp(read,data,CrossoverCampaign.FirstStage+4,current=>{writes++;return true;}),Is.False);
            Assert.That(JsonUtility.ToJson(read),Is.EqualTo(before));Assert.That(writes,Is.EqualTo(1));
        }
    }
}
