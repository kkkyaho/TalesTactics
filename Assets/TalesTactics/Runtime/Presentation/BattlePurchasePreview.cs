using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        int purchasePreviewCharacter;
        CharacterData[] PurchasePreviewRoster(EquipmentData item)=>item==null?Array.Empty<CharacterData>():
            JoinedCharacters().Where(c=>EquipmentLoadout.CanEquip(c,item,item.Slot)).ToArray();
        CharacterData PurchasePreviewRecipient(EquipmentData item)
        {
            var roster=PurchasePreviewRoster(item);
            purchasePreviewCharacter=Mathf.Clamp(purchasePreviewCharacter,0,Math.Max(0,roster.Length-1));
            return roster.Length==0?null:roster[purchasePreviewCharacter];
        }
        void DrawPurchasePreview(EquipmentData item,int page)
        {
            var roster=PurchasePreviewRoster(item);
            var panel=IntermissionPanel(commands,"PurchaseComparison",new Vector2(.025f,.32f),new Vector2(.975f,.55f));
            if(roster.Length==0){FormationText(panel,"장착 가능한 캐릭터가 없습니다",Vector2.zero,Vector2.one,17);return;}
            purchasePreviewCharacter=Mathf.Clamp(purchasePreviewCharacter,0,roster.Length-1);var c=roster[purchasePreviewCharacter];
            FormationButton(panel,"구매 비교 캐릭터","비교 · "+c.DisplayName.Split(' ')[0]+"  ›",new Vector2(.01f,.69f),new Vector2(.99f,.99f),()=>{purchasePreviewCharacter=(purchasePreviewCharacter+1)%roster.Length;RenderMarket(false,page,item);},roster.Length>1,17);
            var progress=FormationProgress(c);var gear=new EquipmentLoadout(c,progress,battle.Catalog.Equipment);
            int level=battle.TrainingMode?25:progress.Level;bool promoted=!battle.TrainingMode&&progress.Promoted;
            var before=gear.Preview(level,promoted);string previous=gear.Get(item.Slot)?.DisplayName??"미장착";
            gear.Equip(item.Slot,item);var after=gear.Preview(level,promoted);
            var equipmentNames=FormationText(panel,previous+" → "+item.DisplayName,new Vector2(.01f,.49f),new Vector2(.99f,.70f),14);
            // This compact metadata row needs the full 22.68px logical height:
            // Noto CJK at the authored 14px has a 20.27px line height.
            equipmentNames.rectTransform.offsetMin=new Vector2(6,0);
            equipmentNames.rectTransform.offsetMax=new Vector2(-6,0);
            equipmentNames.color=formationGold;
            string[] names={"HP","MP","공격","방어","마력","마방","속도","이동","도약"};
            int[] a={before.HP,before.MP,before.STR,before.DEF,before.MAG,before.MDF,before.SPD,before.MOV,before.JMP};
            int[] b={after.HP,after.MP,after.STR,after.DEF,after.MAG,after.MDF,after.SPD,after.MOV,after.JMP};
            var changes=Enumerable.Range(0,9).Where(i=>a[i]!=b[i]).ToArray();
            if(changes.Length==0)FormationText(panel,"능력치 변화 없음",new Vector2(.01f,.02f),new Vector2(.99f,.49f),16);
            for(int i=0;i<changes.Length;i++)
            {
                // Actual same-slot catalog swaps change up to four stats. Keep at
                // most two rows so CJK line height remains readable in this panel.
                int rows=Mathf.Min(2,changes.Length),columns=(changes.Length+rows-1)/rows,index=changes[i];
                float x=(i/rows)/(float)columns,top=.49f-i%rows*.47f/rows;
                var value=FormationText(panel,names[index]+" "+a[index]+" → "+b[index],new Vector2(x,top-.47f/rows),new Vector2(x+1f/columns,top),changes.Length<=2?20:16);
                value.rectTransform.offsetMin=new Vector2(6,0);value.rectTransform.offsetMax=new Vector2(-6,0);value.color=b[index]>a[index]?formationTeal:new Color(1,.63f,.56f);
            }
        }
    }
}
