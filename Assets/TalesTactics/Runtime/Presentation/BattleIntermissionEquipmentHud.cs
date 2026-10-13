using System;
using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void RenderIntermissionEquipment(CharacterData c,EquipmentLoadout draft,string notice)
        {
            if(battle.Session!=null)return;
            selectedCharacter=Array.IndexOf(battle.Catalog.Characters,c);BeginIntermission("장비 정비",1);
            focusedEquipmentLayout=true;commands.gameObject.SetActive(false);IntermissionLayout();
            center.Find("IntermissionBackdrop").gameObject.SetActive(false);
            var progress=FormationProgress(c);var committed=new EquipmentLoadout(c,progress,battle.Catalog.Equipment,battle.Campaign);
            var current=draft??committed;int level=battle.TrainingMode?25:Mathf.Clamp(progress.Level,1,50);bool promoted=!battle.TrainingMode&&progress.Promoted;
            var chars=battle.Catalog.Characters;
            int pages=Math.Max(1,(chars.Length+9)/10);intermissionRosterPage=Mathf.Clamp(intermissionRosterPage,0,pages-1);
            var roster=FormationRect(center,"EquipmentRoster",new Vector2(.01f,.84f),new Vector2(.99f,.995f));
            int first=intermissionRosterPage*10,count=Math.Min(10,chars.Length-first);
            for(int i=0;i<count;i++)
            {
                int index=first+i;var member=chars[index];float width=1f/(pages>1?12:Math.Max(count,1));
                var cell=FormationButton(roster,member.DisplayName,"",new Vector2(i*width,0),new Vector2((i+1)*width,1),()=>{selectedCharacter=index;equipmentPage=0;ShowEquipment(member);});
                IntermissionSelection(cell,member==c);IntermissionPortrait(cell,member,new Vector2(.01f,.38f),new Vector2(.99f,1));
                var caption=FormationText(cell,member.DisplayName.Split(' ')[0],Vector2.zero,new Vector2(1,0),16,TextAlignmentOptions.Center);
                caption.rectTransform.offsetMin=new Vector2(2,0);caption.rectTransform.offsetMax=new Vector2(-2,30);
            }
            if(pages>1)
            {
                FormationButton(roster,"이전 캐릭터","‹",new Vector2(10/12f,0),new Vector2(11/12f,1),()=>{intermissionRosterPage--;RenderEquipment(c,current,notice);},intermissionRosterPage>0);
                FormationButton(roster,"다음 캐릭터","›",new Vector2(11/12f,0),Vector2.one,()=>{intermissionRosterPage++;RenderEquipment(c,current,notice);},intermissionRosterPage+1<pages);
            }
            var profile=IntermissionPanel(center,"EquipmentCharacter",new Vector2(.01f,.015f),new Vector2(.30f,.82f));
            var inventory=FormationRect(center,"EquipmentInventory",new Vector2(.33f,.015f),new Vector2(.98f,.82f));
            IntermissionPortrait(profile,c,new Vector2(.025f,.43f),new Vector2(.975f,.99f));
            FormationText(profile,c.DisplayName,new Vector2(.035f,.34f),new Vector2(.965f,.44f),27).color=formationGold;
            FormationText(profile,(promoted?c.PromotionJob:c.Job)+" · Lv "+level,new Vector2(.035f,.27f),new Vector2(.965f,.34f),18);
            foreach(EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var chosen=slot;var item=current.Get(slot);float top=.27f-(int)slot*.09f;
                var button=FormationButton(profile,SlotName(slot)+": "+(item==null?"없음":item.DisplayName),"",new Vector2(.02f,top-.09f),new Vector2(.98f,top),()=>{selectedEquipmentSlot=chosen;equipmentPage=0;RenderEquipment(c,current,null);});
                IntermissionSelection(button,slot==selectedEquipmentSlot);EquipmentArt(button,item,new Vector2(.01f,0),new Vector2(.16f,1));
                FormationText(button,SlotName(slot)+" · "+(item==null?"없음":item.DisplayName),new Vector2(.18f,0),new Vector2(.99f,1),17);
            }
            FormationText(inventory,SlotName(selectedEquipmentSlot)+" 선택",new Vector2(0,.89f),new Vector2(.48f,1),24).color=formationGold;
            for(int i=0;i<3;i++)
            {
                var slot=(EquipmentSlot)i;var tab=FormationButton(inventory,"분류: "+SlotName(slot),SlotName(slot),new Vector2(.49f+i*.17f,.89f),new Vector2(.66f+i*.17f,1),()=>{selectedEquipmentSlot=slot;equipmentPage=0;RenderEquipment(c,current,null);});IntermissionSelection(tab,slot==selectedEquipmentSlot);
            }
            var options=current.Options(selectedEquipmentSlot);const int pageSize=4;int optionPages=Math.Max(1,(options.Length+pageSize-1)/pageSize);equipmentPage=Mathf.Clamp(equipmentPage,0,optionPages-1);
            var selected=current.Get(selectedEquipmentSlot);var worn=committed.Get(selectedEquipmentSlot);
            for(int i=equipmentPage*pageSize;i<Math.Min(options.Length,(equipmentPage+1)*pageSize);i++)
            {
                var item=options[i];int cell=i%pageSize;float x=cell%2*.5f,top=.88f-cell/2*.245f;
                var row=FormationButton(inventory,"장착 후보: "+item.DisplayName,"",new Vector2(x,top-.23f),new Vector2(x+.5f,top),()=>{current.Equip(selectedEquipmentSlot,item);RenderEquipment(c,current,null);});IntermissionSelection(row,selected==item);
                EquipmentArt(row,item,new Vector2(.01f,.12f),new Vector2(.26f,.88f));
                FormationText(row,item.DisplayName,new Vector2(.28f,.49f),new Vector2(.98f,.96f),22);
                FormationText(row,worn==item?"장착 중":"사용 가능 "+CampaignInventory.Available(battle.Campaign,item.Id,c.Id),new Vector2(.28f,.08f),new Vector2(.98f,.47f),17).color=formationTeal;
            }
            if(options.Length==0)FormationText(inventory,"보유한 호환 장비가 없습니다.\n상점에서 장비를 구매할 수 있습니다.",new Vector2(.03f,.43f),new Vector2(.97f,.82f),23,TextAlignmentOptions.Center);
            FormationButton(inventory,"장비 해제","해제",new Vector2(0,.285f),new Vector2(.25f,.39f),()=>{current.Equip(selectedEquipmentSlot,null);RenderEquipment(c,current,null);});
            if(optionPages>1)
            {
                FormationButton(inventory,"이전 장비 후보","‹ 이전",new Vector2(.40f,.285f),new Vector2(.61f,.39f),()=>{equipmentPage--;RenderEquipment(c,current,null);},equipmentPage>0);
                FormationText(inventory,(equipmentPage+1)+" / "+optionPages,new Vector2(.61f,.285f),new Vector2(.78f,.39f),16,TextAlignmentOptions.Center);
                FormationButton(inventory,"다음 장비 후보","다음 ›",new Vector2(.78f,.285f),new Vector2(1,.39f),()=>{equipmentPage++;RenderEquipment(c,current,null);},equipmentPage+1<optionPages);
            }
            var comparison=FormationRect(inventory,"ChangedStats",Vector2.zero,new Vector2(1,.28f));
            EquipmentChangedStats(comparison,committed.Preview(level,promoted),current.Preview(level,promoted));
            int changed=Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().Count(s=>committed.Get(s)!=current.Get(s));
            IntermissionNotice(notice??(changed>0?"변경 사항 "+changed+"건 · 장착하면 저장됩니다.":"장비를 선택해 비교하세요."));
            if(changed>0)FormationButton(footer,"돌아가기 (미적용 취소)","변경 취소",new Vector2(.54f,.14f),new Vector2(.74f,.86f),()=>ShowEquipment(c));
            else FormationButton(footer,"출전 준비로","거점으로",new Vector2(.54f,.14f),new Vector2(.74f,.86f),ShowIntermissionHub);
            IntermissionPrimary("적용 · 저장",changed>0?"장착하기":"장착 중",()=>
            {
                if(!battle.CanSave)return;bool saved=current.TrySave(battle.Campaign,battle.PersistCampaign);
                RenderEquipment(c,current,saved?"장비를 저장했습니다.":"수량 부족 또는 저장 실패. 기존 장비를 유지했습니다.");
            },changed>0&&battle.CanSave&&current.CanCommit(battle.Campaign));
        }
    }
}
