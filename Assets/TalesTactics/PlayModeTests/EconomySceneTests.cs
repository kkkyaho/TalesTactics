using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator RandomLootKeepsRollAcrossExceptionAndRetryThenPersistsRepeatRewards()
        {
            string path=Path.Combine(Path.GetTempPath(),"TalesEconomy-"+Guid.NewGuid().ToString("N")+".json");
            var file=new CampaignFile(path);int rolls=0,writes=0;
            try
            {
                director.Campaign=new CampaignSave();director.TrainingMode=false;
                director.RewardRoll=()=>{rolls++;return rolls==1?2500:9999;};
                director.PersistCampaign=save=>{writes++;if(writes==1)throw new IOException("simulated");return writes>2&&file.Save(save);};
                director.BeginBattle();yield return null;
                foreach(var enemy in director.Session.Units.Where(u=>u.Team==Team.Enemy))enemy.Damage(99999,director.Session.Grid);
                director.SetState(new TurnStartState(director));yield return null;
                Assert.That(director.RewardPending,Is.True);Assert.That(director.Campaign.Gold,Is.EqualTo(300));
                Click("보상 저장 재시도");yield return null;Assert.That(director.RewardPending,Is.True);
                Click("보상 저장 재시도");yield return null;
                Assert.That(rolls,Is.EqualTo(1));Assert.That(writes,Is.EqualTo(3));Assert.That(director.RewardPending,Is.False);
                Assert.That(director.Message,Does.Contain("가죽 갑옷 +1"));Assert.That(CampaignInventory.Owned(file.Load(),"leather-armor"),Is.EqualTo(1));
                director.SaveBattleReward();Assert.That(writes,Is.EqualTo(3));
                director.Restart();director.BeginBattle();yield return null;
                foreach(var enemy in director.Session.Units.Where(u=>u.Team==Team.Enemy))enemy.Damage(99999,director.Session.Grid);
                director.SetState(new TurnStartState(director));yield return null;
                Assert.That(rolls,Is.EqualTo(2));Assert.That(director.Message,Does.Contain("반복 EXP +60"));
                Assert.That(director.Message,Does.Contain("추가: 없음"));var loaded=file.Load();Assert.That(loaded.Gold,Is.EqualTo(480));
                Assert.That(CampaignInventory.Owned(loaded,"vital-charm"),Is.EqualTo(2));Assert.That(CampaignInventory.Owned(loaded,"leather-armor"),Is.EqualTo(1));
            }
            finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");}
        }
        [UnityTest] public IEnumerator TrainingAndDefeatNeverRollRandomRewards()
        {
            int rolls=0,writes=0;director.Campaign=new CampaignSave();director.RewardRoll=()=>{rolls++;return 0;};director.PersistCampaign=_=>{writes++;return true;};
            director.TrainingMode=true;director.BeginBattle();yield return null;
            foreach(var enemy in director.Session.Units.Where(u=>u.Team==Team.Enemy))enemy.Damage(99999,director.Session.Grid);
            director.SetState(new TurnStartState(director));yield return null;
            director.Restart();director.TrainingMode=false;director.BeginBattle();yield return null;
            foreach(var ally in director.Session.Units.Where(u=>u.Team==Team.Player))ally.Damage(99999,director.Session.Grid);
            director.SetState(new TurnStartState(director));yield return null;
            Assert.That(rolls,Is.Zero);Assert.That(writes,Is.Zero);
        }
    }
}
