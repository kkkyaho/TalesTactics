using System.Linq;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        // Result inspection is read-only. The existing modal controls block the result's
        // Continue button and preserve the mandatory ending and reward transaction.
        void ShowRewardItemDetails(BattleDirector.ItemResult reward,RectTransform source)
        {
            if(reward?.Item==null||battle.RewardPending||resultPanel==null||!resultPanel.gameObject.activeSelf||InputModalOpen)return;
            var unit=battle.Session?.Units.FirstOrDefault(u=>u.Team==Team.Player);
            if(unit==null)return;
            PrepareUnitModal(unit);Clear(unitDetails);
            if(statusBackdrop==null)
            {
                statusBackdrop=Panel("StatusBackdrop",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
                statusBackdrop.GetComponent<UnityEngine.UI.Image>().color=new Color(.005f,.015f,.035f,.65f);
                statusBackdrop.GetComponent<UnityEngine.UI.Outline>().enabled=false;
            }
            statusBackdrop.gameObject.SetActive(true);statusBackdrop.SetAsLastSibling();unitDetails.SetAsLastSibling();
            unitDetails.GetComponent<UnityEngine.UI.Image>().color=new Color(.027f,.045f,.085f,.995f);
            var content=FormationRect(unitDetails,"RewardItemDetailContent",new Vector2(.015f,.015f),new Vector2(.985f,.985f));
            var item=reward.Item;
            FormationText(content,"획득 아이템",new Vector2(.02f,.91f),new Vector2(.82f,.99f),25).color=formationGold;
            EquipmentArt(content,item,new Vector2(.02f,.74f),new Vector2(.23f,.91f));
            FormationText(content,item.DisplayName,new Vector2(.25f,.82f),new Vector2(.98f,.92f),26);
            FormationText(content,SlotName(item.Slot)+" · 획득 "+reward.Count+"개 · 보유 "+CampaignInventory.Owned(battle.Campaign,item.Id)+"개",new Vector2(.25f,.74f),new Vector2(.98f,.83f),18).color=formationTeal;
            var description=FormationText(content,ReadableBonus(item),new Vector2(.03f,.32f),new Vector2(.97f,.72f),21);
            description.name="RewardItemEffects";description.alignment=TMPro.TextAlignmentOptions.TopLeft;
            var eligible=JoinedCharacters().Where(c=>EquipmentLoadout.CanEquip(c,item,item.Slot)).Select(c=>c.DisplayName.Split(' ')[0]).ToArray();
            FormationText(content,"장착 가능한 동료",new Vector2(.02f,.24f),new Vector2(.98f,.32f),18).color=formationGold;
            FormationText(content,eligible.Length==0?"현재 합류한 동료 중 장착 가능한 캐릭터가 없습니다.":string.Join(" · ",eligible),new Vector2(.02f,.10f),new Vector2(.98f,.24f),18).name="RewardEligibleCharacters";
            FormationText(content,"아이템은 보관함에 저장되었습니다.",new Vector2(.02f,0),new Vector2(.98f,.09f),16).color=new Color(.75f,.82f,.85f);
            var close=CloseIcon(unitDetails,"보상 상세 닫기",()=>
            {
                CloseUnitDetails();
                if(source!=null&&source.gameObject.activeInHierarchy)UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(source.gameObject);
            });
            UpdateRewardDetailsLayout();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(close.gameObject);
        }

        void UpdateRewardDetailsLayout()
        {
            if(!UnitDetailsOpen)return;
            var content=unitDetails.Find("RewardItemDetailContent");if(content==null||!content.gameObject.activeSelf)return;
            float scale=Mathf.Clamp(battle.Preferences?.TextScale??1,1,1.3f);
            unitDetails.sizeDelta=new Vector2(Mathf.Min(760*scale,canvas.rect.width-48),Mathf.Min(680*scale,canvas.rect.height-48));
            unitDetails.anchoredPosition=Vector2.zero;
        }
    }
}
