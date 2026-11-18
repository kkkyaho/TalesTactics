using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        static readonly string[] VariedRewardIds={"training-staff","steel-fist","infantry-rifle","training-spear","reinforced-bow","steel-claw","steel-shield","tempered-armor","swift-boots","tempered-armor","focus-charm","tempered-armor","healer-badge","tempered-armor"};
        [Serializable] sealed class RewardCapture
        {
            public int episode,width,height;public string item,file,sha256;
        }
        [Serializable] sealed class RewardCaptureManifest
        {
            public string scope="Editor PlayMode production-catalog result/detail UI. Prerequisite completion flags and victory objectives are fixtures; this is not a Windows capture, natural campaign completion or balance result.";
            public string unityVersion,startedUtc;public bool complete;public RewardCapture[] captures;
        }

        static void AssertRewardIllustration(Transform parent,EquipmentData item)
        {
            var art=parent.GetComponentsInChildren<Transform>().Single(t=>t.name=="EquipmentArt-"+item.Id);
            Assert.That(art.gameObject.activeInHierarchy,Is.True,item.Id);
            if(item.Icon!=null)
            {
                var sprite=art.GetComponent<UnityEngine.UI.Image>();
                Assert.That(sprite,Is.Not.Null);Assert.That(sprite.sprite,Is.SameAs(item.Icon));
                Assert.That(sprite.enabled&&sprite.color.a>0,Is.True,item.Id);
            }
            else
            {
                // Established equipment uses the authored atlas rather than a serialized Sprite.
                var cells=new Dictionary<string,int>{{"guardian-medal",5},{"tempered-armor",6},{"swift-boots",7},{"focus-charm",8},{"healer-badge",5}};
                Assert.That(cells.ContainsKey(item.Id),Is.True,"A missing weapon icon must not pass as a generic fallback: "+item.Id);
                var atlas=Resources.Load<Texture2D>("TalesTactics/EquipmentIcons");Assert.That(atlas,Is.Not.Null);
                var image=art.GetComponentInChildren<UnityEngine.UI.RawImage>();Assert.That(image,Is.Not.Null);
                Assert.That(image.texture,Is.SameAs(atlas));Assert.That(image.enabled&&image.color.a>0,Is.True,item.Id);
                int cell=cells[item.Id];Assert.That(image.uvRect,Is.EqualTo(new Rect(cell%3/3f,(2-cell/3)/3f,1/3f,1/3f)),item.Id);
            }
        }
        [UnityTest,Category("RewardVariety")]
        public IEnumerator VariedMainRewardsRenderEquipAndReloadWithoutReplacingExistingMedals()
        {
            // Only prerequisites and battle outcomes are fixtures. Reward application,
            // rendered buttons, equipment drafts and disk/title reload use production paths.
            PreparationGuidanceEpisode(7);
            director.Campaign.Inventory.Add(new OwnedEquipment{Id="guardian-medal",Count=6});
            director.RewardRoll=()=>9999;
            Assert.That(director.Catalog.Characters.All(director.IsJoined),Is.True);
            Assert.That(director.SavePreparation(),Is.True);
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","RewardVariety20261106","PlayMode-"+Guid.NewGuid().ToString("N")));
            var captures=new List<RewardCapture>();
            var manifest=new RewardCaptureManifest{unityVersion=Application.unityVersion,startedUtc=DateTime.UtcNow.ToString("o")};
            Directory.CreateDirectory(output);
            for(int index=0;index<VariedRewardIds.Length;index++)
            {
                int episode=index+7,stage=CrossoverCampaign.FirstStage+episode-1;
                string id=VariedRewardIds[index];var item=director.Catalog.Equipment.Single(e=>e!=null&&e.Id==id);
                Assert.That(director.SelectedStage,Is.EqualTo(stage));
                int ownedBefore=CampaignInventory.Owned(director.Campaign,id),goldBefore=director.Campaign.Gold;
                director.BeginBattle();yield return null;
                Assert.That(director.Session,Is.Not.Null,"Main reward fixture: "+episode);
                SatisfyObjectiveFixture();yield return null;yield return new WaitForEndOfFrame();
                Assert.That(director.RewardPending,Is.False);
                Assert.That(director.ItemResults.Select(r=>r.Item.Id+":"+r.Count),Is.EqualTo(new[]{id+":1"}));
                Assert.That(CampaignInventory.Owned(director.Campaign,id),Is.EqualTo(ownedBefore+1));
                Assert.That(CampaignInventory.Owned(director.Campaign,"guardian-medal"),Is.EqualTo(6));
                Assert.That(director.Campaign.Gold,Is.EqualTo(goldBefore+120+50*episode));
                Assert.That(director.Campaign.HasPendingAfterStory,Is.True);
                var card=FormationControl("보상 상세: "+id);
                AssertRewardIllustration(card.transform,item);
                Assert.That(card.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text==item.DisplayName+" ×1"),Is.True);
                string awarded=JsonUtility.ToJson(director.Campaign);
                Click(card.name);yield return null;yield return new WaitForEndOfFrame();
                var detail=director.Hud.transform.Find("UnitDetails");
                Assert.That(director.Hud.InputModalOpen,Is.True);
                Assert.That(FormationControl("결과 계속").IsInteractable(),Is.False);
                AssertRewardIllustration(detail,item);
                string expected=string.Join(" · ",director.Catalog.Characters.Where(c=>director.IsJoined(c)&&EquipmentLoadout.CanEquip(c,item,item.Slot)).Select(c=>c.DisplayName.Split(' ')[0]));
                Assert.That(expected,Is.Not.Empty,"Every reward must have a joined compatible owner.");
                Assert.That(detail.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.name=="RewardEligibleCharacters").text,Is.EqualTo(expected));
                foreach(var label in detail.GetComponentsInChildren<TMPro.TMP_Text>())
                {label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,id+" / "+label.text);}
                if(episode==7||episode==14||episode==17)
                {
                    string file="episode-"+episode.ToString("00")+"-reward-detail.png";
                    var image=ScreenCapture.CaptureScreenshotAsTexture();
                    try
                    {
                        byte[] bytes=image.EncodeToPNG();File.WriteAllBytes(Path.Combine(output,file),bytes);
                        using(var sha=System.Security.Cryptography.SHA256.Create())
                            captures.Add(new RewardCapture{episode=episode,item=id,file=file,width=image.width,height=image.height,sha256=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","")});
                    }
                    finally{UnityEngine.Object.Destroy(image);}
                    manifest.captures=captures.ToArray();File.WriteAllText(Path.Combine(output,"manifest.json"),JsonUtility.ToJson(manifest,true));
                }
                Click("보상 상세 닫기");yield return null;
                director.CompleteBattle();director.SaveBattleReward();
                Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(awarded),"Inspection and repeated callbacks cannot apply a second reward.");
                Click("결과 계속");yield return null;
                int lines=0;while(director.StoryActive&&lines++<100){director.AdvanceStory();yield return null;}
                Assert.That(director.StoryActive,Is.False);Assert.That(director.Session,Is.Null);
                Assert.That(director.Campaign.HasPendingAfterStory,Is.False);

                var owner=director.Catalog.Characters.First(c=>director.IsJoined(c)&&EquipmentLoadout.CanEquip(c,item,item.Slot)&&
                    director.Campaign.Characters.Find(p=>p.Id==c.Id)?.Equipment[(int)item.Slot]!=id);
                director.Hud.ShowDeployment();yield return null;Click("장비 관리");yield return null;
                for(int page=0;page<3&&FormationControl("이전 캐릭터").interactable;page++){Click("이전 캐릭터");yield return null;}
                Assert.That(FormationControl("이전 캐릭터").interactable,Is.False);
                for(int page=0;page<3&&!director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name==owner.DisplayName);page++)
                {Click("다음 캐릭터");yield return null;}
                Click(owner.DisplayName);yield return null;
                Click("분류: "+(item.Slot==EquipmentSlot.Weapon?"무기":item.Slot==EquipmentSlot.Armor?"방어구":"장신구"));yield return null;
                string candidate="장착 후보: "+item.DisplayName;
                for(int page=0;page<20&&!director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name==candidate);page++)
                {Click("다음 장비 후보");yield return null;}
                Click(candidate);yield return null;Click("적용 · 저장");yield return null;
                Assert.That(director.Campaign.Get(owner.Id).Equipment[(int)item.Slot],Is.EqualTo(id));
                Assert.That(CampaignInventory.Owned(director.Campaign,id),Is.EqualTo(ownedBefore+1));
                Assert.That(CampaignInventory.Equipped(director.Campaign,id),Is.GreaterThan(0));
                var disk=new CampaignFile(director.Profiles.PathFor(0)).Load();
                Assert.That(JsonUtility.ToJson(disk),Is.EqualTo(JsonUtility.ToJson(director.Campaign)),"Reward and equipment persist through the real campaign file.");
                director.ReturnToTitle();yield return null;
                string beforeReload=JsonUtility.ToJson(director.Campaign);
                Assert.That(director.LoadTitleGame(0,false),Is.True);yield return null;
                Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(beforeReload));
                Assert.That(CampaignInventory.Owned(director.Campaign,"guardian-medal"),Is.EqualTo(6));
            }
            Assert.That(captures.Count,Is.EqualTo(3));manifest.complete=true;manifest.captures=captures.ToArray();
            File.WriteAllText(Path.Combine(output,"manifest.json"),JsonUtility.ToJson(manifest,true));
            TestContext.WriteLine("REWARD_VARIETY_EDITOR_CAPTURE "+output);
        }

        [UnityTest,Category("RewardVariety")]
        public IEnumerator FailedVariedRewardSaveRetriesSameResultAndCommitsOnlyOnce()
        {
            PreparationGuidanceEpisode(7);director.RewardRoll=()=>9999;
            director.Campaign.Inventory.Add(new OwnedEquipment{Id="guardian-medal",Count=6});
            Assert.That(director.SavePreparation(),Is.True);
            director.BeginBattle();yield return null;
            string before=JsonUtility.ToJson(director.Campaign);byte[] diskBefore=File.ReadAllBytes(director.Profiles.PathFor(0));
            var persist=director.PersistCampaign;int attempts=0;
            director.PersistCampaign=save=>++attempts>1&&persist(save);
            SatisfyObjectiveFixture();yield return null;
            Assert.That(director.RewardPending,Is.True);Assert.That(attempts,Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(director.Profiles.PathFor(0)),Is.EqualTo(diskBefore));
            Assert.That(CampaignInventory.Owned(director.Campaign,"training-staff"),Is.Zero);
            Click("보상 저장 재시도");yield return null;
            Assert.That(director.RewardPending,Is.False);Assert.That(attempts,Is.EqualTo(2));
            Assert.That(director.ItemResults.Select(r=>r.Item.Id+":"+r.Count),Is.EqualTo(new[]{"training-staff:1"}));
            Assert.That(FormationControl("보상 상세: training-staff").IsInteractable(),Is.True);
            string after=JsonUtility.ToJson(director.Campaign);director.CompleteBattle();director.SaveBattleReward();
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(after));Assert.That(attempts,Is.EqualTo(2));
            Assert.That(JsonUtility.ToJson(new CampaignFile(director.Profiles.PathFor(0)).Load()),Is.EqualTo(after));
            Assert.That(CampaignInventory.Owned(director.Campaign,"guardian-medal"),Is.EqualTo(6));
        }

        [UnityTest,Category("RewardVariety")]
        public IEnumerator CompletedOldMainSaveKeepsMedalsEquipmentAndDoesNotReceiveRetroactiveRewards()
        {
            TitleSetup();var old=new CampaignSave{Gold=2345,SelectedStage=CrossoverCampaign.LastStage,Deployment=new[]{0,1}};
            for(int episode=1;episode<=20;episode++)old.StoryProgress.Add(CrossoverCampaign.Flag(episode));
            old.StoryProgress.Add("chapter6");old.Inventory.Add(new OwnedEquipment{Id="guardian-medal",Count=20});
            var cless=old.Get("cless");cless.Level=28;cless.EXP=17;cless.Equipment[(int)EquipmentSlot.Accessory]="guardian-medal";
            Assert.That(new CampaignFile(director.Profiles.PathFor(0)).Save(old),Is.True);
            byte[] original=File.ReadAllBytes(director.Profiles.PathFor(0));string expected=JsonUtility.ToJson(new CampaignFile(director.Profiles.PathFor(0)).Load());
            Assert.That(director.LoadTitleGame(0,false),Is.True);yield return null;
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(expected));
            Assert.That(File.ReadAllBytes(director.Profiles.PathFor(0)),Is.EqualTo(original),"Loading must not rewrite or reconcile completed rewards.");
            Assert.That(CampaignInventory.Owned(director.Campaign,"guardian-medal"),Is.EqualTo(20));
            foreach(string id in VariedRewardIds.Distinct())Assert.That(CampaignInventory.Owned(director.Campaign,id),Is.Zero,id);
            Assert.That(FormationControl("전투 시작").interactable,Is.False,"The existing completed main state remains complete.");
            director.Hud.ShowDeployment();yield return null;Click("장비 관리");yield return null;
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(expected),"Browsing equipment cannot migrate old rewards.");
            Assert.That(File.ReadAllBytes(director.Profiles.PathFor(0)),Is.EqualTo(original));
        }
    }
}
