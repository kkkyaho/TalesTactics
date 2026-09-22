using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator VictoryLootRetriesOnceAndTrainingAndDefeatGrantNothing()
        {
            director.Campaign = new CampaignSave(); director.TrainingMode = false;
            int writes = 0; director.PersistCampaign = _ => ++writes > 1;
            director.BeginBattle(); yield return null;
            foreach (var enemy in director.Session.Units.Where(u => u.Team == Team.Enemy)) enemy.Damage(99999, director.Session.Grid);
            director.SetState(new TurnStartState(director)); yield return null;
            string id = CampaignStages.EquipmentReward(0);
            Assert.That(CampaignInventory.Owned(director.Campaign, id), Is.Zero);
            Click("보상 저장 재시도"); yield return null;
            Assert.That(CampaignInventory.Owned(director.Campaign, id), Is.EqualTo(1));
            Assert.That(director.Message.Contains("생명의 부적 +1"), Is.True);
            director.SaveBattleReward(); director.CompleteBattle();
            Assert.That(writes, Is.EqualTo(2));
            director.Restart(); yield return null; director.TrainingMode = true;
            director.BeginBattle(); yield return null;
            foreach (var enemy in director.Session.Units.Where(u => u.Team == Team.Enemy)) enemy.Damage(99999, director.Session.Grid);
            director.SetState(new TurnStartState(director)); yield return null;
            Assert.That(CampaignInventory.Owned(director.Campaign, id), Is.EqualTo(1)); Assert.That(writes, Is.EqualTo(2));
            director.Restart(); yield return null; director.TrainingMode = false;
            director.BeginBattle(); yield return null;
            foreach (var ally in director.Session.Units.Where(u => u.Team == Team.Player)) ally.Damage(99999, director.Session.Grid);
            director.SetState(new TurnStartState(director)); yield return null;
            Assert.That(CampaignInventory.Owned(director.Campaign, id), Is.EqualTo(1)); Assert.That(writes, Is.EqualTo(2));
        }

        [UnityTest] public IEnumerator FullLootInventoryDisplaysReasonAndStillSavesVictory()
        {
            director.Campaign = new CampaignSave(); director.TrainingMode = false;
            string id = CampaignStages.EquipmentReward(0);
            director.Campaign.Inventory.Add(new OwnedEquipment { Id = id, Count = 99 });
            director.PersistCampaign = _ => true;
            director.BeginBattle(); yield return null;
            foreach (var enemy in director.Session.Units.Where(u => u.Team == Team.Enemy)) enemy.Damage(99999, director.Session.Grid);
            director.SetState(new TurnStartState(director)); yield return null;
            Assert.That(director.Message.Contains("미지급 (보유 상한 99개)"), Is.True);
            Assert.That(director.Campaign.Gold, Is.EqualTo(420));
            Assert.That(CampaignInventory.Owned(director.Campaign, id), Is.EqualTo(99));
            Assert.That(director.RewardPending, Is.False);
        }
    }
}
