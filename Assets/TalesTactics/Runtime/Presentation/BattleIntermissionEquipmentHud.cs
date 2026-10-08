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
            if(battle.Session!=null)return;selectedCharacter=Array.IndexOf(battle.Catalog.Characters,c);BeginIntermission("장비 관리",1);
            var progress=FormationProgress(c);var committed=new EquipmentLoadout(c,progress,battle.Catalog.Equipment,battle.Campaign);
            var current=draft??committed;int level=battle.TrainingMode?25:Mathf.Clamp(progress.Level,1,50);bool promoted=!battle.TrainingMode&&progress.Promoted;
            var profile=IntermissionPanel(center,"EquipmentCharacter",Vector2.zero,new Vector2(.44f,1));
            var inventory=IntermissionPanel(center,"EquipmentInventory",new Vector2(.44f,0),Vector2.one);
            FormationText(profile,c.DisplayName,new Vector2(.02f,.92f),new Vector2(.98f,1),25).color=formationGold;
            FormationText(profile,(promoted?c.PromotionJob:c.Job)+" · Lv "+level,new Vector2(.02f,.86f),new Vector2(.98f,.92f),18);
            IntermissionPortrait(profile,c,new Vector2(.025f,.50f),new Vector2(.63f,.85f));
            var model=FormationRect(profile,"EquipmentModel",new Vector2(.60f,.51f),new Vector2(.97f,.83f));
            var sprite=model.gameObject.AddComponent<UnityEngine.UI.Image>();sprite.sprite=c.Sprites?.Front;sprite.preserveAspect=true;sprite.raycastTarget=false;
            foreach(EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var chosen=slot;var item=current.Get(slot);float x=(int)slot/3f;
                var b=FormationButton(profile,SlotName(slot)+": "+(item==null?"없음":item.DisplayName),"",new Vector2(x,.32f),new Vector2(x+1/3f,.50f),()=>{selectedEquipmentSlot=chosen;equipmentPage=0;RenderEquipment(c,current,null);});
                IntermissionSelection(b,slot==selectedEquipmentSlot);EquipmentArt(b,item,new Vector2(.14f,.30f),new Vector2(.86f,.98f));
                FormationText(b,SlotName(slot),Vector2.zero,new Vector2(1,.30f),16,TextAlignmentOptions.Center);
            }
            var roster=FormationRect(profile,"CharacterRoster",Vector2.zero,new Vector2(1,.31f));IntermissionRoster(roster,c,character=>{equipmentPage=0;ShowEquipment(character);});
            FormationText(inventory,"보유 장비",new Vector2(.02f,.92f),new Vector2(.98f,1),24).color=formationGold;
            for(int i=0;i<3;i++)
            {
                var slot=(EquipmentSlot)i;var b=FormationButton(inventory,"분류: "+SlotName(slot),SlotName(slot),new Vector2(i/3f,.84f),new Vector2((i+1)/3f,.92f),()=>{selectedEquipmentSlot=slot;equipmentPage=0;RenderEquipment(c,current,null);});IntermissionSelection(b,slot==selectedEquipmentSlot);
            }
            var options=current.Options(selectedEquipmentSlot);const int pageSize=4;int pages=Math.Max(1,(options.Length+pageSize-1)/pageSize);equipmentPage=Mathf.Clamp(equipmentPage,0,pages-1);
            var selected=current.Get(selectedEquipmentSlot);var worn=committed.Get(selectedEquipmentSlot);
            for(int i=equipmentPage*pageSize;i<Math.Min(options.Length,(equipmentPage+1)*pageSize);i++)
            {
                var item=options[i];float top=.83f-(i%pageSize)*.147f;
                var row=FormationButton(inventory,"장착 후보: "+item.DisplayName,"",new Vector2(.015f,top-.143f),new Vector2(.985f,top),()=>{current.Equip(selectedEquipmentSlot,item);RenderEquipment(c,current,null);});IntermissionSelection(row,selected==item);
                EquipmentArt(row,item,new Vector2(.01f,.03f),new Vector2(.24f,.97f));
                FormationText(row,item.DisplayName,new Vector2(.25f,.51f),new Vector2(.99f,.98f),20);
                string tag=worn==item?"장착 중":"사용 가능 "+CampaignInventory.Available(battle.Campaign,item.Id,c.Id);
                FormationText(row,tag+" · "+ReadableBonus(item).Replace("\n"," / "),new Vector2(.25f,.02f),new Vector2(.99f,.51f),16).color=formationTeal;
            }
            if(options.Length==0)FormationText(inventory,"보유한 호환 장비가 없습니다.\n상점에서 장비를 구매할 수 있습니다.",new Vector2(.03f,.43f),new Vector2(.97f,.77f),19,TextAlignmentOptions.Center);
            FormationButton(inventory,"장비 해제","장비 해제",new Vector2(.015f,.15f),new Vector2(.985f,.235f),()=>{current.Equip(selectedEquipmentSlot,null);RenderEquipment(c,current,null);});
            FormationButton(inventory,"이전 장비 후보","이전",new Vector2(.015f,.075f),new Vector2(.33f,.15f),()=>{equipmentPage--;RenderEquipment(c,current,null);},equipmentPage>0);
            FormationText(inventory,(equipmentPage+1)+" / "+pages,new Vector2(.33f,.075f),new Vector2(.66f,.15f),16,TextAlignmentOptions.Center);
            FormationButton(inventory,"다음 장비 후보","다음",new Vector2(.66f,.075f),new Vector2(.985f,.15f),()=>{equipmentPage++;RenderEquipment(c,current,null);},equipmentPage+1<pages);
            FormationText(inventory,SlotName(selectedEquipmentSlot)+" · 보유한 호환 장비",new Vector2(.02f,0),new Vector2(.98f,.075f),16);
            FormationText(commands,"교체 미리보기",new Vector2(.025f,.92f),new Vector2(.975f,1),25).color=formationGold;
            EquipmentArt(commands,selected,new Vector2(.03f,.72f),new Vector2(.32f,.91f));
            FormationText(commands,selected==null?"장비 없음":selected.DisplayName,new Vector2(.34f,.81f),new Vector2(.98f,.91f),24);
            FormationText(commands,selected==null?"선택한 부위의 장비를 해제합니다.":ReadableBonus(selected),new Vector2(.34f,.70f),new Vector2(.98f,.81f),17).color=formationGold;
            FormationText(commands,(worn==null?"없음":worn.DisplayName)+"  →  "+(selected==null?"없음":selected.DisplayName),new Vector2(.03f,.63f),new Vector2(.97f,.70f),18);
            IntermissionStats(commands,committed.Preview(level,promoted),current.Preview(level,promoted),new Vector2(.025f,.12f),new Vector2(.975f,.63f));
            FormationText(commands,"캐릭터·메뉴 전환 시 미적용 변경은 취소됩니다.",new Vector2(.03f,.01f),new Vector2(.97f,.11f),16);
            int changed=Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().Count(s=>committed.Get(s)!=current.Get(s));
            IntermissionNotice(notice??(changed>0?"변경 사항 "+changed+"건 · 아직 저장되지 않음":"장비를 선택하면 교체 후 능력치를 확인할 수 있습니다."));
            FormationButton(footer,"돌아가기 (미적용 취소)","취소",new Vector2(.53f,.14f),new Vector2(.64f,.86f),()=>ShowEquipment(c));
            FormationButton(footer,"출전 준비로","출전 준비로",new Vector2(.64f,.14f),new Vector2(.75f,.86f),ShowDeployment);
            IntermissionPrimary("적용 · 저장","적용 · 저장",()=>
            {
                if(!battle.CanSave)return;bool saved=current.TrySave(battle.Campaign,battle.PersistCampaign);
                RenderEquipment(c,current,saved?"장비를 저장했습니다.":"수량 부족 또는 저장 실패. 기존 장비를 유지했습니다.");
            },battle.CanSave&&current.CanCommit(battle.Campaign));
        }
    }
}
