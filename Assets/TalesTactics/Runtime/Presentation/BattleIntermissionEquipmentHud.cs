using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        // Detached edits never reserve shared stock. Revalidate availability when committing.
        readonly Dictionary<string,EquipmentLoadout> equipmentDrafts=new Dictionary<string,EquipmentLoadout>();
        CampaignSave equipmentDraftCampaign;
        bool equipmentDraftTraining;
        CharacterData equipmentViewCharacter;
        bool equipmentViewCompact;
        string equipmentViewNotice;
        bool EquipmentCompactLayout=>Screen.width<1200||Screen.width<Screen.height*1.45f;

        void RefreshEquipmentResponsiveLayout()
        {
            if(!focusedEquipmentLayout||battle.Session!=null||InputModalOpen||equipmentViewCharacter==null)return;
            if(EquipmentCompactLayout!=equipmentViewCompact)
                RenderEquipment(equipmentViewCharacter,EquipmentDraft(equipmentViewCharacter),equipmentViewNotice);
        }

        EquipmentLoadout EquipmentDraft(CharacterData character,EquipmentLoadout supplied=null)
        {
            if(!ReferenceEquals(equipmentDraftCampaign,battle.Campaign)||equipmentDraftTraining!=battle.TrainingMode)
            {
                equipmentDrafts.Clear();equipmentDraftCampaign=battle.Campaign;equipmentDraftTraining=battle.TrainingMode;
            }
            if(supplied!=null&&supplied.Character==character)equipmentDrafts[character.Id]=supplied;
            if(!equipmentDrafts.TryGetValue(character.Id,out var draft))
            {
                draft=new EquipmentLoadout(character,FormationProgress(character),battle.Catalog.Equipment,battle.Campaign);
                equipmentDrafts.Add(character.Id,draft);
            }
            return draft;
        }

        int EquipmentDraftChanges(CharacterData character,EquipmentLoadout draft)
        {
            var saved=FormationProgress(character).Equipment;var proposed=draft.ExportIDs();
            return Enumerable.Range(0,3).Count(i=>!string.Equals(saved!=null&&i<saved.Length?saved[i]??"":"",proposed[i]??"",StringComparison.Ordinal));
        }

        Stats EquipmentDraftPreview(CharacterData character,EquipmentLoadout draft,int level,bool promoted)
        {
            // Growth can change while an equipment edit is parked on another screen.
            var progress=FormationProgress(character);
            var preview=new CharacterProgress{Id=character.Id,Equipment=draft.ExportIDs(),Trait=progress.Trait,Growth=progress.Growth};
            return new EquipmentLoadout(character,preview,battle.Catalog.Equipment).Preview(level,promoted);
        }

        void DiscardEquipmentDraft(CharacterData character)
        {
            equipmentDrafts.Remove(character.Id);
            ShowEquipment(character,null,"이 캐릭터의 변경 초안을 취소했습니다.");
        }

        void RenderIntermissionEquipment(CharacterData c,EquipmentLoadout draft,string notice)
        {
            if(battle.Session!=null)return;
            var current=EquipmentDraft(c,draft);
            selectedCharacter=Array.IndexOf(battle.Catalog.Characters,c);BeginIntermission("장비 정비",1);
            focusedEquipmentLayout=true;commands.gameObject.SetActive(false);IntermissionLayout();
            center.Find("IntermissionBackdrop").gameObject.SetActive(false);
            var progress=FormationProgress(c);var committed=new EquipmentLoadout(c,progress,battle.Catalog.Equipment,battle.Campaign);
            int level=battle.TrainingMode?25:Mathf.Clamp(progress.Level,1,50);bool promoted=!battle.TrainingMode&&progress.Promoted;
            bool compact=EquipmentCompactLayout;int changed=EquipmentDraftChanges(c,current);
            equipmentViewCharacter=c;equipmentViewCompact=compact;equipmentViewNotice=notice;
            DrawEquipmentRoster(c,current,notice);

            var profile=IntermissionPanel(center,"EquipmentCharacter",new Vector2(.01f,compact?.635f:.015f),new Vector2(compact?.99f:.26f,.82f));
            var inventory=IntermissionPanel(center,"EquipmentInventory",new Vector2(compact?.01f:.27f,.015f),new Vector2(compact?.48f:.61f,compact?.625f:.82f));
            var comparison=IntermissionPanel(center,"EquipmentComparison",new Vector2(compact?.49f:.62f,.015f),new Vector2(.99f,compact?.625f:.82f));
            DrawEquipmentProfile(profile,c,current,committed,level,promoted,compact);
            DrawEquipmentInventory(inventory,c,current,committed,compact);
            DrawEquipmentComparison(comparison,c,current,committed,level,promoted,changed,compact);

            bool available=current.CanCommit(battle.Campaign);
            string state=changed>0?"변경 사항 "+changed+"건 · 저장 전 초안 · 캐릭터 전환 시 유지":"장착 중인 장비입니다. 후보를 선택해 비교하세요.";
            if(changed>0&&!available)state="사용 가능한 수량이 부족합니다. 초안은 유지되며 저장할 수 없습니다.";
            IntermissionNotice(notice??state);
            if(changed>0)FormationButton(footer,"돌아가기 (미적용 취소)","변경 취소",new Vector2(.54f,.14f),new Vector2(.74f,.86f),()=>DiscardEquipmentDraft(c));
            else FormationButton(footer,"출전 준비로","거점으로",new Vector2(.54f,.14f),new Vector2(.74f,.86f),ShowIntermissionHub);
            IntermissionPrimary("적용 · 저장",changed>0?"장착하고 저장":"장착 중",()=>
            {
                if(!battle.CanSave)return;
                bool saved=current.TrySave(battle.Campaign,battle.PersistCampaign);
                if(saved)equipmentDrafts.Remove(c.Id);
                RenderEquipment(c,saved?null:current,saved?"장비를 장착하고 저장했습니다.":"수량 부족 또는 저장 실패. 기존 장비와 변경 초안을 유지했습니다.");
            },changed>0&&battle.CanSave&&available);
        }

        void DrawEquipmentRoster(CharacterData selected,EquipmentLoadout current,string notice)
        {
            var chars=JoinedCharacters();int pages=Math.Max(1,(chars.Length+5)/6);intermissionRosterPage=Mathf.Clamp(intermissionRosterPage,0,pages-1);
            var roster=FormationRect(center,"EquipmentRoster",new Vector2(.01f,.84f),new Vector2(.99f,.995f));
            int first=intermissionRosterPage*6,count=Math.Min(6,chars.Length-first);float width=(pages>1?.84f:1)/Math.Max(6,count);
            for(int i=0;i<count;i++)
            {
                var member=chars[first+i];int index=Array.IndexOf(battle.Catalog.Characters,member);
                var cell=FormationButton(roster,member.DisplayName,"",new Vector2(i*width,0),new Vector2((i+1)*width,1),()=>{selectedCharacter=index;equipmentPage=0;ShowEquipment(member);});
                IntermissionSelection(cell,member==selected);IntermissionPortrait(cell,member,new Vector2(.02f,.34f),new Vector2(.98f,1));
                bool pending=equipmentDrafts.TryGetValue(member.Id,out var parked)&&EquipmentDraftChanges(member,parked)>0;
                var caption=FormationText(cell,member.DisplayName.Split(' ')[0]+(pending?" · 초안":""),Vector2.zero,new Vector2(1,.34f),16,TextAlignmentOptions.Center);
                if(pending)caption.color=formationGold;
            }
            if(pages>1)
            {
                FormationButton(roster,"이전 캐릭터","이전",new Vector2(.84f,0),new Vector2(.92f,1),()=>{intermissionRosterPage--;RenderEquipment(selected,current,notice);},intermissionRosterPage>0,16);
                FormationButton(roster,"다음 캐릭터","다음",new Vector2(.92f,0),Vector2.one,()=>{intermissionRosterPage++;RenderEquipment(selected,current,notice);},intermissionRosterPage+1<pages,16);
            }
        }

        void DrawEquipmentProfile(RectTransform profile,CharacterData c,EquipmentLoadout draft,EquipmentLoadout committed,int level,bool promoted,bool compact)
        {
            var model=FormationRect(profile,"EquippedModelPreview",compact?new Vector2(.005f,.02f):new Vector2(.03f,.50f),compact?new Vector2(.13f,.98f):new Vector2(.97f,.98f),4);
            var modelImage=model.gameObject.AddComponent<CharacterModelImage>();modelImage.Character=c;modelImage.preserveAspect=true;
            modelImage.sprite=EquipmentAppearance.Resolve(c,draft.Get(EquipmentSlot.Weapon),c.Sprites?.Front);modelImage.raycastTarget=false;
            FormationText(profile,c.DisplayName,compact?new Vector2(.13f,.48f):new Vector2(.035f,.39f),compact?new Vector2(.40f,.96f):new Vector2(.965f,.49f),compact?20:27).color=formationGold;
            FormationText(profile,(promoted?c.PromotionJob:c.Job)+" · Lv "+level,compact?new Vector2(.13f,.04f):new Vector2(.035f,.32f),compact?new Vector2(.40f,.48f):new Vector2(.965f,.39f),compact?16:18);
            foreach(EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var chosen=slot;var item=committed.Get(slot);float top=.30f-(int)slot*.10f,x=.41f+(int)slot*.195f;
                var button=FormationButton(profile,SlotName(slot)+": "+(item==null?"없음":item.DisplayName),"",compact?new Vector2(x,0):new Vector2(.02f,top-.10f),compact?new Vector2(x+.195f,1):new Vector2(.98f,top),()=>{selectedEquipmentSlot=chosen;equipmentPage=0;RenderEquipment(c,draft,null);});
                IntermissionSelection(button,slot==selectedEquipmentSlot);
                var artMin=compact?new Vector2(.26f,.42f):new Vector2(.01f,.05f);
                var artMax=compact?new Vector2(.74f,.98f):new Vector2(.19f,.95f);
                if(item!=null)EquipmentArt(button,item,artMin,artMax);
                else if(slot==EquipmentSlot.Weapon)ClassArt(button,c,artMin,artMax,new Color(.65f,.72f,.78f));
                else FormationIcon(button,slot==EquipmentSlot.Armor?"armor":"accessory",artMin,artMax,new Color(.50f,.55f,.58f));
                FormationText(button,SlotName(slot)+" · "+(item==null?"없음":item.DisplayName),compact?new Vector2(.01f,.01f):new Vector2(.21f,0),compact?new Vector2(.99f,.43f):new Vector2(.99f,1),compact?15:17,compact?TextAlignmentOptions.Center:TextAlignmentOptions.MidlineLeft);
            }
        }

        void DrawEquipmentInventory(RectTransform parent,CharacterData c,EquipmentLoadout draft,EquipmentLoadout committed,bool compact)
        {
            FormationText(parent,"보유 장비",new Vector2(.02f,.90f),new Vector2(.98f,1),compact?21:25).color=formationGold;
            for(int i=0;i<3;i++)
            {
                var slot=(EquipmentSlot)i;var tab=FormationButton(parent,"분류: "+SlotName(slot),SlotName(slot),new Vector2(i/3f,.77f),new Vector2((i+1)/3f,.90f),()=>{selectedEquipmentSlot=slot;equipmentPage=0;RenderEquipment(c,draft,null);});IntermissionSelection(tab,slot==selectedEquipmentSlot);
            }
            // Show owned compatible items even when another ally is using the last copy.
            // This presentation list never relaxes EquipmentLoadout's availability checks.
            var options=(battle.Catalog.Equipment??Array.Empty<EquipmentData>()).Where(item=>item!=null&&EquipmentLoadout.CanEquip(c,item,selectedEquipmentSlot)&&CampaignInventory.Owned(battle.Campaign,item.Id)>0).ToArray();
            // Three readable lines need taller cards after both parent frames apply their insets.
            // Keep two rows in wide and compact layouts so resizing cannot shrink a third row below its text height.
            var usable=draft.Options(selectedEquipmentSlot);const int pageSize=2;
            int pages=Math.Max(1,(options.Length+pageSize-1)/pageSize);equipmentPage=Mathf.Clamp(equipmentPage,0,pages-1);
            var selected=draft.Get(selectedEquipmentSlot);var worn=committed.Get(selectedEquipmentSlot);float rowHeight=.61f/pageSize;
            for(int i=equipmentPage*pageSize;i<Math.Min(options.Length,(equipmentPage+1)*pageSize);i++)
            {
                var item=options[i];float top=.76f-i%pageSize*rowHeight;
                bool canChoose=usable.Contains(item);
                var row=FormationButton(parent,"장착 후보: "+item.DisplayName,"",new Vector2(0,top-rowHeight),new Vector2(1,top),()=>{draft.Equip(selectedEquipmentSlot,item);RenderEquipment(c,draft,null);},canChoose);IntermissionSelection(row,selected==item);
                EquipmentArt(row,item,new Vector2(.01f,.06f),new Vector2(.20f,.94f));
                FormationText(row,item.DisplayName,new Vector2(.21f,.65f),new Vector2(.98f,.98f),compact?17:21);
                var state=FormationText(row,EquipmentInventoryState(c,item,worn,selected),new Vector2(.21f,.29f),new Vector2(.98f,.66f),compact?13:15);
                state.name="EquipmentWearerState";state.color=item==worn?formationGold:canChoose?formationTeal:new Color(.75f,.78f,.81f);
                string quantity="보유 "+CampaignInventory.Owned(battle.Campaign,item.Id)+" · 미장착 "+CampaignInventory.Available(battle.Campaign,item.Id);
                FormationText(row,quantity,new Vector2(.21f,.01f),new Vector2(.98f,.30f),compact?13:15).color=new Color(.75f,.82f,.85f);
            }
            if(options.Length==0)FormationText(parent,"보유한 호환 장비가 없습니다.\n상점에서 구매할 수 있습니다.",new Vector2(.04f,.19f),new Vector2(.96f,.73f),19,TextAlignmentOptions.Center);
            FormationButton(parent,"장비 해제","해제",new Vector2(0,.01f),new Vector2(.28f,.14f),()=>{draft.Equip(selectedEquipmentSlot,null);RenderEquipment(c,draft,null);},true,17);
            if(pages>1)
            {
                FormationButton(parent,"이전 장비 후보","이전",new Vector2(.30f,.01f),new Vector2(.53f,.14f),()=>{equipmentPage--;RenderEquipment(c,draft,null);},equipmentPage>0,16);
                FormationText(parent,(equipmentPage+1)+" / "+pages,new Vector2(.53f,.01f),new Vector2(.75f,.14f),15,TextAlignmentOptions.Center);
                FormationButton(parent,"다음 장비 후보","다음",new Vector2(.75f,.01f),new Vector2(1,.14f),()=>{equipmentPage++;RenderEquipment(c,draft,null);},equipmentPage+1<pages,16);
            }
        }

        string EquipmentInventoryState(CharacterData current,EquipmentData item,EquipmentData worn,EquipmentData selected)
        {
            var states=new List<string>();
            if(item==worn)states.Add("현재 장착");
            else if(item==selected)states.Add("선택 초안");
            var wearers=JoinedCharacters().Where(c=>c!=current&&battle.Campaign.Characters.Any(p=>p.Id==c.Id&&p.Equipment!=null&&p.Equipment.Contains(item.Id)))
                .Select(c=>c.DisplayName.Split(' ')[0]).ToArray();
            if(wearers.Length>0)states.Add(wearers[0]+(wearers.Length>1?" 외 "+(wearers.Length-1)+"명":"")+" 착용 중");
            else if(CampaignInventory.Equipped(battle.Campaign,item.Id,current.Id)>0)states.Add("다른 동료 착용 중");
            return states.Count>0?string.Join(" · ",states):"장착 가능";
        }

        void DrawEquipmentComparison(RectTransform parent,CharacterData c,EquipmentLoadout draft,EquipmentLoadout committed,int level,bool promoted,int changed,bool compact)
        {
            FormationText(parent,changed>0?"장착 비교 · 저장 전 초안":"장착 비교 · 적용 중",new Vector2(.02f,.88f),new Vector2(.98f,1),compact?18:23).color=changed>0?formationTeal:formationGold;
            var before=IntermissionPanel(parent,"CommittedEquipment",new Vector2(.01f,.50f),new Vector2(.49f,.87f));
            var after=IntermissionPanel(parent,"DraftEquipment",new Vector2(.51f,.50f),new Vector2(.99f,.87f));
            EquipmentComparisonItem(before,committed.Get(selectedEquipmentSlot),"현재 장착",compact);
            EquipmentComparisonItem(after,draft.Get(selectedEquipmentSlot),"선택 후보",compact);
            var stats=FormationRect(parent,"ChangedStats",new Vector2(.01f,.01f),new Vector2(.99f,.49f));
            DrawEquipmentStatChanges(stats,c,committed.Preview(level,promoted),EquipmentDraftPreview(c,draft,level,promoted),compact);
        }

        void EquipmentComparisonItem(RectTransform parent,EquipmentData item,string title,bool compact)
        {
            // Share the body horizontally so bilingual weapon names can use its full height.
            // Keep the title and body inside the existing frame without reducing readable type.
            FormationText(parent,title,new Vector2(.01f,.70f),new Vector2(.99f,.94f),compact?15:18,TextAlignmentOptions.Center);
            if(item!=null)
            {
                EquipmentArt(parent,item,new Vector2(.02f,.10f),new Vector2(.26f,.67f));
                FormationText(parent,item.DisplayName.Replace(" / ","\n"),new Vector2(.28f,.06f),new Vector2(.99f,.69f),compact?15:18,TextAlignmentOptions.Center);
            }
            else FormationText(parent,"없음",new Vector2(.01f,.06f),new Vector2(.99f,.31f),compact?15:18,TextAlignmentOptions.Center);
        }

        void DrawEquipmentStatChanges(RectTransform parent,CharacterData c,Stats before,Stats after,bool compact)
        {
            string[] names={"HP","MP","공격","마력","방어","마방","민첩","이동","도약"};
            int[] a={before.HP,before.MP,before.STR,before.MAG,before.DEF,before.MDF,before.SPD,before.MOV,before.JMP};
            int[] b={after.HP,after.MP,after.STR,after.MAG,after.DEF,after.MDF,after.SPD,after.MOV,after.JMP};
            var changes=Enumerable.Range(0,names.Length).Where(i=>a[i]!=b[i]).ToArray();
            FormationText(parent,"능력치 변화",new Vector2(.02f,.79f),new Vector2(.98f,.99f),compact?17:21).color=formationGold;
            for(int row=0;row<Math.Min(3,changes.Length);row++)
            {
                int i=changes[row];float top=.79f-row*.19f;
                FormationText(parent,StatChange(names[i],a[i],b[i]),new Vector2(.02f,top-.19f),new Vector2(.98f,top),compact?17:22);
            }
            if(changes.Length==0)FormationText(parent,"능력치 변화 없음",new Vector2(.02f,.27f),new Vector2(.98f,.76f),compact?18:22);
            FormationButton(parent,"전체 능력치 보기",changes.Length>3?"전체 비교 · 추가 "+(changes.Length-3)+"개":"전체 능력치 보기",new Vector2(.01f,0),new Vector2(.99f,.21f),()=>
            {
                PrepareUnitModal(new UnitRuntime(c,Team.Player,battle.Catalog.Rules));Clear(unitDetails);
                FormationText(unitDetails,"능력치 비교",new Vector2(.04f,.86f),new Vector2(.96f,.98f),25).color=formationGold;
                IntermissionStats(unitDetails,before,after,new Vector2(.04f,.16f),new Vector2(.96f,.86f));
                FormationButton(unitDetails,"능력치 비교 닫기","닫기",new Vector2(.5f,.025f),new Vector2(.96f,.145f),()=>CloseUnitDetails());
            },true,compact?15:18);
        }
    }
}
