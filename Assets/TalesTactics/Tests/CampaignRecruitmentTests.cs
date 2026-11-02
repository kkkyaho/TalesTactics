using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        BattleCatalog RecruitmentCatalog()
        {
            var catalog=New<BattleCatalog>();catalog.CrossoverReady=true;catalog.Rules=rules;rules.MaxDeployment=6;
            catalog.Characters=new[]{"cless","mint","alphen","shionne","farah","tear","jade","natalia","velvet","kisara"}
                .Select(id=>{var c=New<CharacterData>();c.Id=id;return c;}).ToArray();return catalog;
        }
        CampaignSave BeforeRecruitment(int episode)
        {var save=new CampaignSave();for(int n=1;n<episode;n++)save.StoryProgress.Add(CrossoverCampaign.Flag(n));return save;}

        [TestCase(2,2)] [TestCase(3,4)] [TestCase(4,5)] [TestCase(5,8)]
        public void RecruitmentPreparationHidesTheWholeIntroductionGroupWithoutCreatingProgress(int episode,int priorCount)
        {
            var catalog=RecruitmentCatalog();var save=BeforeRecruitment(episode);string before=JsonUtility.ToJson(save);
            Assert.That(catalog.Characters.Count(c=>CampaignRecruitment.Joined(save,c.Id)),Is.EqualTo(priorCount));
            Assert.That(catalog.Characters.Where(c=>CrossoverCampaign.Introduction(c.Id)==episode).All(c=>!CampaignRecruitment.Joined(save,c.Id)),Is.True);
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
        }
        [Test] public void RecruitmentSimultaneousArrivalsUseTheSameFloorAverageIncludingReserves()
        {
            var catalog=RecruitmentCatalog();var save=BeforeRecruitment(4);
            int[] levels={4,7,8,9,10};for(int i=0;i<5;i++)save.Get(catalog.Characters[i].Id).Level=levels[i];
            save.Get("velvet").Level=50; // Future records must not bias the previous roster average.
            var skill=Skill(EffectKind.Damage);skill.Id="recruit-skill";skill.UnlockLevel=7;
            foreach(var c in catalog.Characters.Where(c=>CrossoverCampaign.Introduction(c.Id)==4))c.Skills=new[]{skill};
            var file=new CampaignFile(SaveTestPath());int writes=0;
            Assert.That(CampaignRecruitment.TryBegin(save,catalog,9,s=>{writes++;return file.Save(s);}),Is.True);
            foreach(string id in new[]{"tear","jade","natalia"})
            {Assert.That(save.Get(id).Level,Is.EqualTo(7));Assert.That(save.Get(id).EXP,Is.Zero);Assert.That(save.Get(id).UnlockedSkills,Contains.Item(skill.Id));}
            Assert.That(writes,Is.EqualTo(1));var loaded=file.Load();Assert.That(loaded.Version,Is.EqualTo(3));
            Assert.That(CampaignRecruitment.Joined(loaded,"jade"),Is.True);Assert.That(CampaignRecruitment.Joined(loaded,"velvet"),Is.False);
            save.Get("cless").Level=50;
            Assert.That(CampaignRecruitment.TryBegin(save,catalog,9,s=>{writes++;return true;}),Is.True);
            Assert.That(writes,Is.EqualTo(1));Assert.That(save.Get("jade").Level,Is.EqualTo(7),"Retry must not recompute recruitment growth.");
        }
        [TestCase(false)] [TestCase(true)]
        public void RecruitmentFailedSaveRollsBackFlagsLevelsAndNewRecords(bool throws)
        {
            var catalog=RecruitmentCatalog();var save=BeforeRecruitment(2);save.Get("cless").Level=9;save.Get("mint").Level=10;
            var old=save.Get("alphen");old.EXP=27;old.Equipment[0]="legacy";old.UnlockedSkills.Add("legacy-skill");
            var skills=old.UnlockedSkills;var characters=save.Characters;string before=JsonUtility.ToJson(save);
            Assert.That(CampaignRecruitment.TryBegin(save,catalog,7,s=>{if(throws)throw new InvalidOperationException("fixture");return false;}),Is.False);
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));Assert.That(save.Characters,Is.SameAs(characters));
            Assert.That(save.Get("alphen"),Is.SameAs(old));Assert.That(old.UnlockedSkills,Is.SameAs(skills));
        }
        [Test] public void RecruitmentKeepsExistingGrowthAndCompletedLegacySavesUntouched()
        {
            var catalog=RecruitmentCatalog();var save=BeforeRecruitment(2);save.Get("cless").Level=5;save.Get("mint").Level=6;
            var old=save.Get("alphen");old.Level=18;old.EXP=51;old.Promoted=true;old.Equipment[0]="legacy";old.UnlockedSkills.Add("legacy-skill");
            Assert.That(CampaignRecruitment.TryBegin(save,catalog,7,_=>true),Is.True);
            Assert.That(old.Level,Is.EqualTo(18));Assert.That(old.EXP,Is.EqualTo(51));Assert.That(old.Promoted,Is.True);
            Assert.That(old.Equipment[0],Is.EqualTo("legacy"));Assert.That(old.UnlockedSkills,Contains.Item("legacy-skill"));
            var completed=BeforeRecruitment(6);completed.Get("velvet").Level=2;string before=JsonUtility.ToJson(completed);
            Assert.That(CampaignRecruitment.TryBegin(completed,catalog,10,_=>throw new Exception("must not save")),Is.True);
            Assert.That(JsonUtility.ToJson(completed),Is.EqualTo(before));Assert.That(catalog.Characters.All(c=>CampaignRecruitment.Joined(completed,c.Id)),Is.True);
        }
        [Test] public void RecruitmentReservesSlotsForAllDebutantsAndLaterLeavesSelectionAlone()
        {
            var catalog=RecruitmentCatalog();var save=BeforeRecruitment(5);var chosen=new[]{0,1,2,3,4,6};string before=JsonUtility.ToJson(save);
            var result=CampaignRecruitment.BattleDeployment(save,catalog,10,chosen);
            Assert.That(result,Is.EqualTo(new[]{0,1,2,3,8,9}));Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            save.StoryProgress.Add(CrossoverCampaign.Flag(5));
            Assert.That(CampaignRecruitment.BattleDeployment(save,catalog,11,chosen),Is.EqualTo(chosen));
            Assert.That(CampaignRecruitment.BattleDeployment(save,catalog,10,chosen),Is.EqualTo(chosen),"Completed introduction replays are freely formed.");
        }
        [TestCase(false)] [TestCase(true)]
        public void RecruitmentOldCheckpointVisibilitySurvivesConsumptionAndFailedConsumptionRollsBack(bool throws)
        {
            var save=BeforeRecruitment(2);save.SuspendedBattle=new BattleCheckpoint{Stage=7};save.HasSuspendedBattle=true;
            save.Get("alphen").Level=18;save.Get("alphen").EXP=51;save.Get("shionne").Level=13;
            var checkpoint=save.SuspendedBattle;
            Assert.That(CampaignRecruitment.Joined(save,"alphen"),Is.True);string before=JsonUtility.ToJson(save);
            bool consumedAtWrite=false,joinedAtWrite=false;int writes=0;
            Assert.That(CampaignRecruitment.TryConsumeCheckpoint(save,s=>
            {
                writes++;consumedAtWrite=s.SuspendedBattle==null&&!s.HasSuspendedBattle;
                joinedAtWrite=s.StoryProgress.Contains(CampaignRecruitment.Flag(2));
                if(throws)throw new InvalidOperationException("fixture");return false;
            }),Is.False);
            Assert.That(writes,Is.EqualTo(1));Assert.That(consumedAtWrite&&joinedAtWrite,Is.True);
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));Assert.That(save.SuspendedBattle,Is.SameAs(checkpoint));
            Assert.That(CampaignRecruitment.TryConsumeCheckpoint(save,_=>true),Is.True);
            Assert.That(save.SuspendedBattle,Is.Null);Assert.That(save.HasSuspendedBattle,Is.False);
            Assert.That(save.Get("alphen").Level,Is.EqualTo(18));Assert.That(save.Get("alphen").EXP,Is.EqualTo(51));Assert.That(save.Get("shionne").Level,Is.EqualTo(13));
            Assert.That(CampaignRecruitment.Joined(save,"alphen"),Is.True);Assert.That(CampaignRecruitment.Joined(save,"farah"),Is.False);
        }
        [Test] public void RecruitmentHiddenPresetCannotWriteOrDeployFutureCompanions()
        {
            var catalog=RecruitmentCatalog();var save=BeforeRecruitment(2);save.Presets[0]=new FormationPreset{Members=new[]{"cless","alphen"}};
            string before=JsonUtility.ToJson(save);int writes=0;
            Assert.That(FormationPresets.Apply(save,catalog,0,_=>{writes++;return true;},out var party),Is.Not.Null);
            Assert.That(party,Is.Null);Assert.That(writes,Is.Zero);Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
        }
    }
}
