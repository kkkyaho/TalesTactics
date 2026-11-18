using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void MainRewardVarietyMatchesActualCatalogPreviewAndTwentySavedVictories()
        {
            string[] expected={
                "guardian-medal","guardian-medal","guardian-medal","guardian-medal","guardian-medal","guardian-medal",
                "training-staff","steel-fist","infantry-rifle","training-spear","reinforced-bow","steel-claw","steel-shield",
                "tempered-armor","swift-boots","tempered-armor","focus-charm","tempered-armor","healer-badge","tempered-armor"
            };
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            Assert.That(catalog,Is.Not.Null);
            var save=new CampaignSave();var file=new CampaignFile(SaveTestPath());
            for(int number=1;number<=20;number++)
            {
                int stage=CrossoverCampaign.FirstStage+number-1;
                string context="Main episode "+number;
                var item=catalog.Equipment.Single(e=>e.Id==expected[number-1]);
                var chapter=CampaignStages.Get(stage);
                Assert.That(chapter.Equipment,Is.EqualTo(item.Id),context);
                Assert.That(chapter.EquipmentName,Is.EqualTo(item.DisplayName),context);
                AssertRewardEquipmentArt(item,context);
                Assert.That(catalog.Characters.Any(c=>CrossoverCampaign.Joined(c.Id,number)&&EquipmentLoadout.CanEquip(c,item,item.Slot)),Is.True,context);
                if(number>=7)Assert.That(CampaignInventory.PurchaseUnlockRequirement(save,item),Is.Null,context);
                Assert.That(chapter.EnemyLevel,Is.EqualTo(2*number-1),context);
                Assert.That(chapter.EntryLevel,Is.EqualTo(Math.Max(1,2*number-3)),context);
                Assert.That(chapter.FirstEXP,Is.EqualTo(35),context);
                Assert.That(chapter.RepeatEXP,Is.EqualTo(10),context);
                Assert.That(chapter.FirstGold,Is.EqualTo(120+50*number),context);
                Assert.That(chapter.RepeatGold,Is.EqualTo((120+50*number)/2),context);

                string before=JsonUtility.ToJson(save);
                Assert.That(CampaignEconomy.Preview(save,stage),Does.Contain("최초 보상:").And.Contain("확정: "+item.DisplayName+" 1개"),context);
                Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before),"Preview must not grant or save a reward: "+context);
                int previousCount=CampaignInventory.Owned(save,item.Id),previousGold=save.Gold;
                var reward=CampaignEconomy.Prepare(save,stage,9999);
                Assert.That(CampaignStages.TryReward(save,reward,new[]{data},file.Save),Is.True,context);
                Assert.That(CampaignStages.TryReward(save,reward,new[]{data},file.Save),Is.False,context);
                save=file.Load();
                Assert.That(CampaignInventory.Owned(save,item.Id),Is.EqualTo(previousCount+1),context);
                Assert.That(save.Gold,Is.EqualTo(previousGold+120+50*number),context);
                Assert.That(save.StoryProgress.Count,Is.EqualTo(number),context);
                Assert.That(CampaignEconomy.Preview(save,stage),Does.Contain("반복 보상:").And.Contain("확정: "+item.DisplayName+" 1개"),context);
            }
            Assert.That(save.Version,Is.EqualTo(3));
            Assert.That(save.Gold,Is.EqualTo(13200));
            Assert.That(save.Get(data.Id).Level,Is.EqualTo(8));
            Assert.That(save.Get(data.Id).EXP,Is.Zero);
            Assert.That(CampaignInventory.Owned(save,"bronze-sword"),Is.EqualTo(1));
            foreach(var group in expected.GroupBy(id=>id))
                Assert.That(CampaignInventory.Owned(save,group.Key),Is.EqualTo(group.Count()),group.Key);
            Assert.That(save.Inventory.Count,Is.EqualTo(expected.Distinct().Count()+1));
            Assert.That(save.StoryProgress.All(flag=>flag.StartsWith("crossover.chapter",StringComparison.Ordinal)),Is.True);
        }

        static void AssertRewardEquipmentArt(EquipmentData item,string context)
        {
            // Exercise the same production sprite/atlas selection used by result and
            // equipment cards. Keep the isolated hierarchy inactive in EditMode so
            // UI material lifecycle callbacks do not allocate shared play-mode state.
            var root=new GameObject("Reward equipment art probe",typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var hud=root.AddComponent<BattleHud>();
                var draw=typeof(BattleHud).GetMethod("EquipmentArt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                Assert.That(draw,Is.Not.Null);
                draw.Invoke(hud,new object[]{root.transform,item,Vector2.zero,Vector2.one});
                var art=root.transform.Find("EquipmentArt-"+item.Id);
                Assert.That(art,Is.Not.Null,context);
                if(item.Icon!=null)
                {
                    var image=art.GetComponent<UnityEngine.UI.Image>();
                    Assert.That(image,Is.Not.Null,context);
                    Assert.That(image.sprite,Is.SameAs(item.Icon),context);
                    Assert.That(image.sprite.texture,Is.Not.Null,context);
                    Assert.That(image.raycastTarget,Is.False,context);
                }
                else
                {
                    int cell;
                    switch(item.Id)
                    {
                        case "guardian-medal":case "healer-badge":cell=5;break;
                        case "tempered-armor":cell=6;break;
                        case "swift-boots":cell=7;break;
                        case "focus-charm":cell=8;break;
                        default:throw new AssertionException("Missing authored reward sprite: "+item.Id);
                    }
                    var image=art.GetComponentInChildren<UnityEngine.UI.RawImage>(true);
                    var atlas=Resources.Load<Texture2D>("TalesTactics/EquipmentIcons");
                    Assert.That(atlas,Is.Not.Null,context);
                    Assert.That(image,Is.Not.Null,context);
                    Assert.That(image.texture,Is.SameAs(atlas),context);
                    Assert.That(image.uvRect,Is.EqualTo(new Rect(cell%3/3f,(2-cell/3)/3f,1/3f,1/3f)),context);
                    Assert.That(image.raycastTarget,Is.False,context);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }

        [TestCase(7,"training-staff")]
        [TestCase(14,"tempered-armor")]
        [TestCase(17,"focus-charm")]
        public void VariedRewardFailureRetryDuplicateAndRepeatKeepInventoryAndGrowthAtomic(int number,string itemId)
        {
            var save=new CampaignSave();
            for(int n=1;n<number;n++)save.StoryProgress.Add(CrossoverCampaign.Flag(n));
            var existing=new OwnedEquipment{Id=itemId,Count=2};save.Inventory.Add(existing);
            var medals=new OwnedEquipment{Id="guardian-medal",Count=6};save.Inventory.Add(medals);
            int stage=CrossoverCampaign.FirstStage+number-1;
            var reward=CampaignEconomy.Prepare(save,stage,0); // Keep the separate bronze-sword drop.
            string before=JsonUtility.ToJson(save);var inventory=save.Inventory;
            Assert.That(CampaignStages.TryReward(save,reward,new[]{data},_=>false),Is.False);
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            Assert.Throws<IOException>(()=>CampaignStages.TryReward(save,reward,new[]{data},_=>throw new IOException("test")));
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            Assert.That(save.Inventory,Is.SameAs(inventory));
            Assert.That(save.Inventory.Find(e=>e.Id==itemId),Is.SameAs(existing));
            Assert.That(save.Inventory.Find(e=>e.Id=="guardian-medal"),Is.SameAs(medals));
            Assert.That(reward.Applied,Is.False);

            var file=new CampaignFile(SaveTestPath());
            Assert.That(CampaignStages.TryReward(save,reward,new[]{data},file.Save),Is.True);
            string after=JsonUtility.ToJson(save);
            Assert.That(CampaignStages.TryReward(save,reward,new[]{data},file.Save),Is.False);
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(after));
            save=file.Load();
            Assert.That(CampaignInventory.Owned(save,itemId),Is.EqualTo(3));
            Assert.That(CampaignInventory.Owned(save,"guardian-medal"),Is.EqualTo(6));
            Assert.That(CampaignInventory.Owned(save,"bronze-sword"),Is.EqualTo(2));
            Assert.That(save.Get(data.Id).EXP,Is.EqualTo(35));
            int previousGold=save.Gold;
            var repeat=CampaignEconomy.Prepare(save,stage,9999);
            Assert.That(repeat.Repeat,Is.True);
            Assert.That(CampaignStages.TryReward(save,repeat,new[]{data},file.Save),Is.True);
            Assert.That(CampaignStages.TryReward(save,repeat,new[]{data},file.Save),Is.False);
            save=file.Load();
            Assert.That(CampaignInventory.Owned(save,itemId),Is.EqualTo(4));
            Assert.That(CampaignInventory.Owned(save,"guardian-medal"),Is.EqualTo(6));
            Assert.That(CampaignInventory.Owned(save,"bronze-sword"),Is.EqualTo(2));
            Assert.That(save.Gold,Is.EqualTo(previousGold+(120+50*number)/2));
            Assert.That(save.Get(data.Id).EXP,Is.EqualTo(45));
            Assert.That(save.StoryProgress.Count,Is.EqualTo(number));
        }

        [Test] public void CompletedMainSaveKeepsOldMedalsAndEquipmentWithoutRetroactiveRewardExchange()
        {
            var save=new CampaignSave();
            for(int number=1;number<=20;number++)save.StoryProgress.Add(CrossoverCampaign.Flag(number));
            save.Inventory.Add(new OwnedEquipment{Id="guardian-medal",Count=20});
            var character=save.Get(data.Id);character.Level=31;character.EXP=42;character.Equipment[(int)EquipmentSlot.Accessory]="guardian-medal";
            var file=new CampaignFile(SaveTestPath());Assert.That(file.Save(save),Is.True);
            save=file.Load();string before=JsonUtility.ToJson(save);
            for(int stage=CrossoverCampaign.FirstStage;stage<=CrossoverCampaign.LastStage;stage++)
                Assert.That(CampaignEconomy.Preview(save,stage),Does.StartWith("반복 보상:"));
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            Assert.That(file.Save(save),Is.True);save=file.Load();
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            Assert.That(CampaignInventory.Owned(save,"guardian-medal"),Is.EqualTo(20));
            Assert.That(save.Inventory.Count,Is.EqualTo(2));
            Assert.That(save.Get(data.Id).Equipment[(int)EquipmentSlot.Accessory],Is.EqualTo("guardian-medal"));
            Assert.That(save.Get(data.Id).Level,Is.EqualTo(31));
            Assert.That(save.Get(data.Id).EXP,Is.EqualTo(42));
            Assert.That(save.StoryProgress.Count,Is.EqualTo(20));
        }

        [Test] public void FullVariedRewardInventoryKeepsEquippedCopyAndSeparateDrop()
        {
            var save=new CampaignSave();
            for(int number=1;number<14;number++)save.StoryProgress.Add(CrossoverCampaign.Flag(number));
            var full=new OwnedEquipment{Id="tempered-armor",Count=CampaignInventory.MaxQuantity};save.Inventory.Add(full);
            save.Get(data.Id).Equipment[(int)EquipmentSlot.Armor]=full.Id;
            int stage=CrossoverCampaign.FirstStage+13;
            var reward=CampaignEconomy.Prepare(save,stage,2500);
            string before=JsonUtility.ToJson(save);
            Assert.That(CampaignStages.TryReward(save,reward,new[]{data},_=>false),Is.False);
            Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            var file=new CampaignFile(SaveTestPath());
            Assert.That(CampaignStages.TryReward(save,reward,new[]{data},file.Save),Is.True);
            save=file.Load();
            Assert.That(CampaignInventory.Owned(save,"tempered-armor"),Is.EqualTo(CampaignInventory.MaxQuantity));
            Assert.That(CampaignInventory.Owned(save,"reinforced-armor"),Is.EqualTo(1));
            Assert.That(save.Get(data.Id).Equipment[(int)EquipmentSlot.Armor],Is.EqualTo("tempered-armor"));
            Assert.That(save.Get(data.Id).EXP,Is.EqualTo(35));
            Assert.That(save.Gold,Is.EqualTo(1120));
            CampaignInventory.Validate(save);
        }
    }
}
