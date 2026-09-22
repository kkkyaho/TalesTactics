using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
namespace TalesTactics.Editor
{
    public static class EconomyBalance
    {
        [MenuItem("Tales Tactics/Apply Economy Baseline")]
        public static void Apply()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            var armor=catalog.Equipment.Single(e=>e.Id=="reinforced-armor");
            // Migrate only the previous prototype price; preserve any custom value.
            if(armor.BuyPrice==280){armor.BuyPrice=300;EditorUtility.SetDirty(armor);AssetDatabase.SaveAssets();}
            Export();
        }
        [MenuItem("Tales Tactics/Export Economy Report")]
        public static void Export()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            var report=new StringBuilder("# 경제 기준선 실측표\n\n현재 에셋 기준 가격. 매각은 구매가의 절반(소수점 버림).\n\n|장비|구매G|매각G|해금|\n|---|---:|---:|---|\n");
            foreach(var item in catalog.Equipment)report.AppendLine($"|{item.DisplayName}|{item.BuyPrice}|{item.BuyPrice/2}|{(string.IsNullOrEmpty(item.RequiredStoryFlag)?"기본":item.RequiredStoryFlag)}|");
            report.AppendLine("\n|장|최초 EXP/G|반복 EXP/G|확정|추가 장비 매각 기대G|\n|---|---|---|---|---:|");
            for(int stage=0;stage<2;stage++)
            {
                decimal expected=0;
                for(int roll=0;roll<10000;roll++){string id=CampaignEconomy.Drop(stage,roll);if(id!=null)expected+=catalog.Equipment.Single(e=>e.Id==id).BuyPrice/2;}
                report.AppendLine($"|{stage+1}|{CampaignEconomy.Experience(stage,false)} / {CampaignEconomy.Gold(stage,false)}|{CampaignEconomy.Experience(stage,true)} / {CampaignEconomy.Gold(stage,true)}|{CampaignStages.EquipmentReward(stage)}|{(expected/10000m).ToString(System.Globalization.CultureInfo.InvariantCulture)}|");
            }
            report.AppendLine("\n확률 보상은 승리당 한 번만 추첨(청동검25%, 장별 방어구15%, 없음60%). 확정 보상과 별개다. 위 기대값은 재고 상한 이전의 추가 장비 매각 가치이며 실제 골드 지급액이 아니다.\n\n성장 곡선: 아래 CSV는 1장 최초 → 2장 최초 → 2장 반복18회, 구매/매각/추가 드롭 없이 검증한 값이다. 장비를 얻어도 자동 매각하지 않는다. 레벨당 필요 EXP는 기존 Level×100, 상한50을 유지한다.\n");
            File.WriteAllText("Docs/ECONOMY_BALANCE.md",report.ToString());
            var rows=new StringBuilder("clear,stage,repeat,expReward,goldReward,level,exp,gold\n");var save=new CampaignSave();var party=new[]{catalog.Characters[0]};
            for(int i=0;i<20;i++)
            {
                int stage=i==0?0:1;var reward=CampaignEconomy.Prepare(save,stage,9999);
                if(!CampaignStages.TryReward(save,reward,party,_=>true))throw new System.InvalidOperationException("Reward simulation failed");
                var p=save.Get(party[0].Id);rows.AppendLine($"{i+1},{stage+1},{reward.Repeat},{reward.Experience},{reward.Gold},{p.Level},{p.EXP},{save.Gold}");
            }
            File.WriteAllText("Docs/economy-progression.csv",rows.ToString());
        }
    }
}
