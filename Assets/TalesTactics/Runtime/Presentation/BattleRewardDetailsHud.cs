using System.Collections;
using System.Linq;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform rewardDetailsSource,rewardDetailsContent;
        int rewardFocusRevision;

        // Inspection uses a detached loadout, never the intermission draft or save.
        void ShowRewardItemDetails(BattleDirector.ItemResult reward,RectTransform source)
        {
            if(reward?.Item==null||battle.RewardPending||resultPanel==null||!resultPanel.gameObject.activeSelf||InputModalOpen)return;
            var unit=battle.Session?.Units.FirstOrDefault(u=>u.Team==Team.Player);
            if(unit==null)return;
            PrepareUnitModal(unit);Clear(unitDetails);rewardDetailsSource=source;
            if(statusBackdrop==null)
            {
                statusBackdrop=Panel("StatusBackdrop",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
                statusBackdrop.GetComponent<UnityEngine.UI.Image>().color=new Color(.005f,.015f,.035f,.65f);
                statusBackdrop.GetComponent<UnityEngine.UI.Outline>().enabled=false;
            }
            statusBackdrop.gameObject.SetActive(true);statusBackdrop.SetAsLastSibling();unitDetails.SetAsLastSibling();
            unitDetails.GetComponent<UnityEngine.UI.Image>().color=new Color(.027f,.045f,.085f,.995f);
            var content=rewardDetailsContent=FormationRect(unitDetails,"RewardItemDetailContent",new Vector2(.015f,.015f),new Vector2(.985f,.985f));
            var item=reward.Item;
            EquipmentArt(content,item,new Vector2(.02f,.84f),new Vector2(.14f,.99f));
            FormationText(content,"획득 아이템 · "+item.DisplayName,new Vector2(.15f,.91f),new Vector2(.89f,.99f),27).color=formationGold;
            FormationText(content,SlotName(item.Slot)+" · 획득 "+reward.Count+"개 · 보유 "+CampaignInventory.Owned(battle.Campaign,item.Id)+"개",new Vector2(.15f,.84f),new Vector2(.89f,.91f),18).color=formationTeal;
            FormationText(content,"획득 장비 효과",new Vector2(.03f,.77f),new Vector2(.40f,.84f),20).color=formationGold;
            RewardComparisonText(content,"RewardItemEffects",ReadableBonus(item),new Vector2(.03f,.49f),new Vector2(.40f,.77f),20);
            FormationText(content,"현재 장비 효과",new Vector2(.03f,.42f),new Vector2(.40f,.49f),20).color=formationGold;
            var currentEffects=RewardComparisonText(content,"RewardComparisonCurrentEffects","",new Vector2(.03f,.18f),new Vector2(.40f,.42f),20);

            var roster=JoinedCharacters().Where(c=>EquipmentLoadout.CanEquip(c,item,item.Slot)).ToArray();
            FormationText(content,roster.Length==0?"현재 합류한 동료 중 장착 가능한 캐릭터가 없습니다.":string.Join(" · ",roster.Select(c=>c.DisplayName.Split(' ')[0])),new Vector2(.03f,.075f),new Vector2(.97f,.15f),16).name="RewardEligibleCharacters";
            FormationText(content,"비교 후 장착은 인터미션의 장비 관리에서 할 수 있습니다.",new Vector2(.03f,.005f),new Vector2(.97f,.07f),17).color=new Color(.75f,.82f,.85f);
            var comparison=FormationRect(content,"RewardComparison",new Vector2(.44f,.18f),new Vector2(.97f,.74f));
            var recipient=FormationText(content,"",new Vector2(.53f,.75f),new Vector2(.88f,.84f),21,TMPro.TextAlignmentOptions.Center);
            recipient.name="RewardComparisonCharacter";
            int selected=0;
            System.Action refresh=()=>
            {
                Clear(comparison);
                if(roster.Length==0)
                {
                    recipient.text="비교할 동료 없음";
                    currentEffects.text="미장착";
                    RewardComparisonText(comparison,"RewardComparisonEquipment","비교할 동료가 없습니다.",Vector2.zero,Vector2.one,21);
                    return;
                }
                var character=roster[selected];var progress=FormationProgress(character);
                var gear=new EquipmentLoadout(character,progress,battle.Catalog.Equipment);
                var current=gear.Get(item.Slot);int level=battle.TrainingMode?25:progress.Level;
                bool promoted=!battle.TrainingMode&&progress.Promoted;
                var before=gear.Preview(level,promoted);
                recipient.text=character.DisplayName.Split(' ')[0]+" · Lv"+level+"  ("+(selected+1)+"/"+roster.Length+")";
                currentEffects.text=current==null?"미장착":ReadableBonus(current);
                string names=(current?.DisplayName??"미장착")+" → "+item.DisplayName;
                if(current!=null&&current.Id==item.Id)names+="\n이미 같은 장비를 착용 중입니다.";
                RewardComparisonText(comparison,"RewardComparisonEquipment",names,new Vector2(0,.77f),Vector2.one,19).color=formationGold;
                bool equipped=gear.Equip(item.Slot,item);
                FormationText(comparison,"능력치 · 현재 → 교체 후",new Vector2(0,.67f),new Vector2(1,.77f),18).color=formationTeal;
                RewardComparisonText(comparison,"RewardComparisonStats",equipped?RewardComparisonStats(before,gear.Preview(level,promoted)):"이 장비의 능력치를 비교할 수 없습니다.",Vector2.zero,new Vector2(1,.67f),20);
            };
            var previous=FormationButton(content,"이전 보상 비교 동료","이전",new Vector2(.44f,.75f),new Vector2(.53f,.84f),()=>{selected=(selected+roster.Length-1)%roster.Length;refresh();},roster.Length>1,18);
            var next=FormationButton(content,"다음 보상 비교 동료","다음",new Vector2(.88f,.75f),new Vector2(.97f,.84f),()=>{selected=(selected+1)%roster.Length;refresh();},roster.Length>1,18);
            var close=CloseIcon(unitDetails,"보상 상세 닫기",()=>CloseUnitDetails());
            // Keep keyboard/controller focus in this modal, including after cycling.
            var controls=roster.Length>1?new[]{close,previous,next}:new[]{close};
            for(int i=0;i<controls.Length;i++)
            {
                var navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit};
                navigation.selectOnUp=navigation.selectOnLeft=controls[(i+controls.Length-1)%controls.Length].GetComponent<UnityEngine.UI.Button>();
                navigation.selectOnDown=navigation.selectOnRight=controls[(i+1)%controls.Length].GetComponent<UnityEngine.UI.Button>();
                controls[i].GetComponent<UnityEngine.UI.Button>().navigation=navigation;
            }
            refresh();UpdateRewardDetailsLayout();
            FocusRewardDetailsControl(close);
        }

        void FocusRewardDetailsControl(RectTransform control)
        {
            var button=control!=null?control.GetComponent<UnityEngine.UI.Button>():null;
            if(button==null||!button.gameObject.activeInHierarchy||!button.IsInteractable())return;
            foreach(var frame in control.GetComponentsInParent<FramedContent>())frame.Fit();
            var pointer=battle.GetComponent<GamepadPointer>();
            if(pointer!=null)pointer.FocusButton(button);
            else UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(control.gameObject);
            StartCoroutine(RefreshRewardDetailsFocus(button,++rewardFocusRevision));
        }
        IEnumerator RefreshRewardDetailsFocus(UnityEngine.UI.Button button,int revision)
        {
            // A rebuilt modal can receive its final frame padding in the next LateUpdate.
            yield return null;
            yield return new WaitForEndOfFrame();
            if(revision!=rewardFocusRevision||button==null||!button.gameObject.activeInHierarchy||!button.IsInteractable()||UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject!=button.gameObject)yield break;
            battle.GetComponent<GamepadPointer>()?.FocusButton(button);
        }
        TMPro.TMP_Text RewardComparisonText(Transform parent,string name,string value,Vector2 min,Vector2 max,int size)
        {
            var text=FormationText(parent,value,min,max,size,TMPro.TextAlignmentOptions.TopLeft);text.name=name;return text;
        }
        static string RewardComparisonStats(Stats before,Stats after)
        {
            string[] names={"HP","MP","공격","마력","방어","마방","민첩","이동","도약"};
            int[] a={before.HP,before.MP,before.STR,before.MAG,before.DEF,before.MDF,before.SPD,before.MOV,before.JMP};
            int[] b={after.HP,after.MP,after.STR,after.MAG,after.DEF,after.MDF,after.SPD,after.MOV,after.JMP};
            return string.Join("\n",Enumerable.Range(0,names.Length).Select(i=>
            {
                int delta=b[i]-a[i];string line=names[i]+" "+a[i]+" → "+b[i]+" ("+(delta>0?"+":"")+delta+")";
                return delta==0?line:"<color="+(delta>0?"#77D7BD":"#FFA18F")+">"+line+"</color>";
            }));
        }
        void UpdateRewardDetailsLayout()
        {
            if(!UnitDetailsOpen)return;
            // Clear disables old children before deferred destruction; Find can still return that old namesake.
            if(rewardDetailsContent==null||!rewardDetailsContent.gameObject.activeSelf||rewardDetailsContent.parent!=unitDetails)return;
            float scale=Mathf.Clamp(battle.Preferences?.TextScale??1,1,1.3f);
            unitDetails.sizeDelta=new Vector2(Mathf.Min(1100*scale,canvas.rect.width-48),Mathf.Min(850*scale,canvas.rect.height-48));
            unitDetails.anchoredPosition=Vector2.zero;
        }
    }
}
