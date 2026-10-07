using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ExpansionPagesUnlockAndAllStoriesAndMapsFit()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;director.Hud.ShowChapterPage(0);yield return null;
            Click("다음 장 목록");yield return null;
            Assert.That(director.SelectedStage,Is.Zero);Assert.That(director.Hud.ChapterPage,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name.Contains(CampaignStages.Title(3))).interactable,Is.False);
            var party=director.Deployment.Select(i=>director.Catalog.Characters[i]).ToArray();
            for(int stage=0;stage<3;stage++)CampaignStages.TryReward(director.Campaign,stage,party,_=>true);
            director.PersistCampaign=_=>true;director.RewardRoll=()=>9999;
            for(int stage=3;stage<6;stage++)
            {
                director.Hud.ShowDeployment();yield return null;
                Click(CampaignStages.Title(stage));yield return null;
                foreach(var button in Object.FindObjectsByType<UnityEngine.UI.Button>())
                {
                    var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                    Assert.That(corners.All(c=>c.x>=0&&c.x<=Screen.width&&c.y>=0&&c.y<=Screen.height),Is.True,button.name);
                }
                director.RequestBattle();yield return null;
                while(director.StoryActive)
                {
                    foreach(var label in Object.FindObjectsByType<TMPro.TMP_Text>().Where(t=>t.transform.IsChildOf(director.Hud.transform.Find("CampaignStory"))))
                    {label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text);Assert.That(label.font.HasCharacters(label.text,out uint[] missing,true,true),Is.True,label.text);}
                    director.AdvanceStory();yield return null;
                }
                Assert.That(director.Session.Units.Where(u=>u.Team==Team.Enemy).All(u=>u.Level==CampaignStages.EnemyLevel(stage)),Is.True);
                foreach(var tile in director.Session.Grid.Tiles.Values)
                {
                    var p=director.Board.BattleCamera.WorldToScreenPoint(tile.WorldPosition(director.Catalog.Rules.TileHeight));
                    Assert.That(director.Board.BattleCamera.pixelRect.Contains(new Vector2(p.x,p.y)),Is.True);
                }
                foreach(var enemy in director.Session.Units.Where(u=>u.Team==Team.Enemy))enemy.Damage(99999,director.Session.Grid);
                if(director.Session.Victory is SurviveTurns survival){Assert.That(director.Session.Result,Is.EqualTo(BattleResult.Ongoing));for(int n=0;n<12;n++)survival.OnTurnEnded(director.Session.Units[0]);}
                if(director.Session.Objective==ObjectiveKind.Escort){Assert.That(director.Session.Result,Is.EqualTo(BattleResult.Ongoing));director.Session.Grid.Place(director.Session.ObjectiveUnit,director.Session.Destination);}
                director.SetState(new TurnStartState(director));yield return null;
                Assert.That(director.CanReadEnding,Is.True);director.ReadEnding();yield return null;
                while(director.StoryActive)
                {
                    foreach(var label in Object.FindObjectsByType<TMPro.TMP_Text>().Where(t=>t.transform.IsChildOf(director.Hud.transform.Find("CampaignStory"))))
                    {label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text);Assert.That(label.font.HasCharacters(label.text,out uint[] missing,true,true),Is.True,label.text);}
                    director.AdvanceStory();yield return null;
                }
                director.Restart();yield return null;
            }
            Click("이전 장 목록");yield return null;Assert.That(director.Hud.ChapterPage,Is.Zero);
            Assert.That(director.SelectedStage,Is.EqualTo(5)); // Browsing never silently changes selected battle.
        }

        [UnityTest] public IEnumerator ExpansionEquipmentUnlocksPurchasesAndPersistsStats()
        {
            var path=Path.Combine(Application.temporaryCachePath,"TalesExpansion",System.Guid.NewGuid().ToString("N"),"campaign.json");
            var file=new CampaignFile(path);director.Campaign=new CampaignSave();director.TrainingMode=false;director.PersistCampaign=file.Save;
            var medal=director.Catalog.Equipment.Single(e=>e.Id=="guardian-medal");var armor=director.Catalog.Equipment.Single(e=>e.Id=="tempered-armor");
            Assert.That(CampaignInventory.PurchaseUnlockRequirement(director.Campaign,medal),Is.Not.Null);
            var party=director.Deployment.Select(i=>director.Catalog.Characters[i]).ToArray();
            for(int stage=0;stage<4;stage++)CampaignStages.TryReward(director.Campaign,stage,party,file.Save);
            Assert.That(CampaignInventory.PurchaseUnlockRequirement(director.Campaign,medal),Is.Null);
            Assert.That(CampaignInventory.PurchaseUnlockRequirement(director.Campaign,armor),Is.Not.Null);
            director.Hud.ShowDeployment();yield return null;Click("장비 상점");yield return null;
            Click(medal.DisplayName+" · "+medal.BuyPrice+"G");yield return null;Click("구매 · 저장");yield return null;
            Assert.That(CampaignInventory.Owned(file.Load(),medal.Id),Is.EqualTo(2));
            CampaignStages.TryReward(director.Campaign,4,party,file.Save);
            Assert.That(CampaignInventory.PurchaseUnlockRequirement(director.Campaign,armor),Is.Null);
            var progress=director.Campaign.Get(party[0].Id);progress.Equipment[(int)EquipmentSlot.Accessory]=medal.Id;progress.Equipment[(int)EquipmentSlot.Armor]=armor.Id;
            Assert.That(file.Save(director.Campaign),Is.True);director.Campaign=file.Load();director.SelectedStage=5;director.BeginBattle();yield return null;
            var unit=director.Session.Units.Single(u=>u.Data.Id==party[0].Id);
            Assert.That(unit.Stats.HP,Is.EqualTo(party[0].StatsAt(7).HP+120));
            Assert.That(unit.Stats.DEF,Is.EqualTo(party[0].StatsAt(7).DEF+10));
            Assert.That(unit.Stats.MDF,Is.EqualTo(party[0].StatsAt(7).MDF+8));
        }
    }
}
