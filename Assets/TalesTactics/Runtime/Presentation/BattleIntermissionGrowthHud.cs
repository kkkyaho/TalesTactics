using System;
using TMPro;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void RenderIntermissionGrowth(CharacterData c,string notice,TacticalTrait? choice=null)
        {
            if(battle.Session!=null)return;selectedCharacter=Array.IndexOf(battle.Catalog.Characters,c);BeginIntermission("성장 · 승급",3);
            var progress=FormationProgress(c);var trait=choice??progress.Trait;
            var gear=new EquipmentLoadout(c,progress,battle.Catalog.Equipment,battle.Campaign);
            var before=gear.Preview(progress.Level,progress.Promoted);var after=gear.Preview(progress.Level,true);
            var profile=IntermissionPanel(center,"GrowthCharacter",Vector2.zero,new Vector2(.46f,1));
            var promotion=IntermissionPanel(center,"Promotion",new Vector2(.46f,0),Vector2.one);
            FormationText(profile,"캐릭터 정보",new Vector2(.02f,.92f),new Vector2(.98f,1),24).color=formationGold;
            IntermissionPortrait(profile,c,new Vector2(.02f,.59f),new Vector2(.50f,.92f));
            FormationText(profile,c.DisplayName,new Vector2(.51f,.81f),new Vector2(.99f,.92f),23);
            FormationText(profile,progress.Promoted?c.PromotionJob:c.Job,new Vector2(.51f,.72f),new Vector2(.99f,.81f),18).color=formationGold;
            FormationText(profile,"Lv "+progress.Level,new Vector2(.51f,.61f),new Vector2(.99f,.72f),32);
            FormationText(profile,progress.Level>=50?"EXP MAX":"EXP  "+progress.EXP.ToString("N0")+" / "+(progress.Level*100).ToString("N0"),new Vector2(.025f,.54f),new Vector2(.975f,.60f),18);
            var track=IntermissionPanel(profile,"ExperienceTrack",new Vector2(.025f,.505f),new Vector2(.975f,.54f));
            var fill=FormationRect(track,"ExperienceFill",Vector2.zero,new Vector2(progress.Level>=50?1:Mathf.Clamp01(progress.EXP/(progress.Level*100f)),1));var fillImage=fill.gameObject.AddComponent<UnityEngine.UI.Image>();fillImage.color=formationGold;fillImage.raycastTarget=false;
            FormationText(profile,progress.Level>=50?"최대 레벨 달성":"다음 레벨까지 "+Mathf.Max(0,progress.Level*100-progress.EXP),new Vector2(.025f,.445f),new Vector2(.975f,.505f),17);
            FormationText(profile,"HP "+before.HP+"    MP "+before.MP+"\n공격 "+before.STR+"    방어 "+before.DEF+"\n마력 "+before.MAG+"    마방 "+before.MDF+"\n민첩 "+before.SPD+"    이동 "+before.MOV+"    도약 "+before.JMP,new Vector2(.025f,.29f),new Vector2(.975f,.445f),18);
            IntermissionRoster(FormationRect(profile,"CharacterRoster",Vector2.zero,new Vector2(1,.29f)),c,character=>ShowGrowth(character));
            FormationText(promotion,"승급",new Vector2(.025f,.92f),new Vector2(.975f,1),24).color=formationGold;
            FormationIcon(promotion,FormationWeaponIcon(c),new Vector2(.04f,.81f),new Vector2(.19f,.91f),formationGold);
            FormationText(promotion,c.Job+"  →  "+c.PromotionJob,new Vector2(.20f,.81f),new Vector2(.98f,.92f),21);
            bool story=battle.Campaign.StoryProgress.Contains(c.PromotionStoryFlag);bool level=progress.Level>=c.PromotionLevel;
            FormationText(promotion,(level?"충족":"미충족")+" · Lv "+c.PromotionLevel+" 달성 (현재 Lv "+progress.Level+")\n"+(story?"충족":"미충족")+" · "+(c.PromotionStoryFlag=="chapter2"?"제2장 완료":c.PromotionStoryFlag),new Vector2(.025f,.72f),new Vector2(.975f,.81f),18);
            IntermissionStats(promotion,before,after,new Vector2(.025f,.20f),new Vector2(.975f,.72f));
            string reason=CampaignPromotion.Unavailable(progress,c,battle.Campaign.StoryProgress);
            if(battle.TrainingMode)reason="훈련 모드에서는 승급할 수 없습니다.";else if(!battle.CanSave)reason="저장 보호 상태에서는 승급할 수 없습니다.";
            FormationText(promotion,reason??"승급할 수 있습니다.",new Vector2(.025f,.13f),new Vector2(.975f,.20f),16);
            FormationButton(promotion,progress.Promoted?"승급 완료":"승급 · 저장",progress.Promoted?"승급 완료":"승급 · 저장",new Vector2(.025f,.065f),new Vector2(.53f,.13f),()=>
            {
                if(battle.TrainingMode||!battle.CanSave)return;bool saved=CampaignPromotion.TrySave(battle.Campaign,c,battle.PersistCampaign);
                ShowGrowth(c,saved?"승급을 저장했습니다. 다음 캠페인 전투에 적용됩니다.":"승급하지 못했습니다. 조건과 저장 상태를 확인하세요.");
            },reason==null);
            int target=CampaignStages.Get(battle.SelectedStage).EntryLevel;
            FormationButton(promotion,"합류 훈련 · Lv"+target,"합류 훈련 · Lv"+target,new Vector2(.53f,.065f),new Vector2(.975f,.13f),()=>
            {
                if(battle.TrainingMode||!battle.CanSave)return;bool saved=TacticalDevelopment.CatchUp(battle.Campaign,c,battle.SelectedStage,battle.PersistCampaign);
                ShowGrowth(c,saved?"합류 훈련을 저장했습니다.":"훈련을 저장하지 못했습니다. 기존 성장을 유지합니다.");
            },progress.Level<target&&CampaignStages.Unlocked(battle.Campaign,battle.SelectedStage)&&!battle.TrainingMode&&battle.CanSave);
            FormationButton(promotion,"캐릭터 목록으로","선택 초기화",new Vector2(.025f,0),new Vector2(.50f,.065f),()=>ShowGrowth(c));
            FormationButton(promotion,"기술 숙련 안내","기술 숙련 안내",new Vector2(.50f,0),new Vector2(.975f,.065f),()=>ShowMastery(c));
            FormationText(commands,"전술 특성",new Vector2(.025f,.92f),new Vector2(.60f,1),25).color=formationGold;
            FormationText(commands,"한 번에 하나 적용",new Vector2(.60f,.92f),new Vector2(.98f,1),16);
            string[] icons={"wait","move","guard","skill","heal"};
            foreach(TacticalTrait value in Enum.GetValues(typeof(TacticalTrait)))
            {
                var option=value;float top=.91f-(int)value*.158f;
                var card=FormationButton(commands,"특성 선택: "+TacticalDevelopment.TraitName(value),"",new Vector2(.025f,top-.15f),new Vector2(.975f,top),()=>RenderIntermissionGrowth(c,null,option));IntermissionSelection(card,trait==value);
                FormationIcon(card,icons[(int)value],new Vector2(.03f,.23f),new Vector2(.17f,.77f),trait==value?formationTeal:formationGold);
                FormationText(card,(trait==value?"● ":"")+TacticalDevelopment.TraitName(value)+(progress.Trait==value?" · 적용 중":""),new Vector2(.20f,.62f),new Vector2(.98f,.99f),20).color=trait==value?formationTeal:formationGold;
                string effect=value==TacticalTrait.Protector?"방어 중 인접 아군 피해 -30%\n자기 턴마다 1회 · 이동 -1":TacticalDevelopment.TraitDescription(value).Replace("MOV","이동").Replace("DEF","방어").Replace("STR","공격");
                FormationText(card,effect,new Vector2(.20f,.02f),new Vector2(.98f,.62f),16);
            }
            FormationText(commands,"준비 중 무료 변경 · 저장 후 다음 전투부터 적용",new Vector2(.025f,.015f),new Vector2(.975f,.11f),17).color=formationTeal;
            IntermissionNotice(notice??(battle.TrainingMode?"훈련 중에는 저장된 성장만 조회합니다.":!battle.CanSave?"저장 보호 상태입니다.":trait!=progress.Trait?"특성 변경 · 아직 저장되지 않음":"저장된 캐릭터의 성장 정보를 표시합니다."));
            FormationButton(footer,"출전 준비로","출전 준비로",new Vector2(.55f,.14f),new Vector2(.74f,.86f),ShowDeployment);
            IntermissionPrimary("특성 적용 · 저장","특성 적용 · 저장",()=>
            {
                if(battle.TrainingMode||!battle.CanSave)return;bool saved=TacticalDevelopment.SaveTrait(battle.Campaign,c,trait,battle.PersistCampaign);
                RenderIntermissionGrowth(c,saved?"특성을 저장했습니다. 다음 전투부터 적용됩니다.":"저장 실패. 기존 특성을 유지합니다.",saved?(TacticalTrait?)null:trait);
            },!battle.TrainingMode&&battle.CanSave&&trait!=progress.Trait);
        }
        void ShowMastery(CharacterData c)
        {
            var p=FormationProgress(c);var u=new UnitRuntime(c,Team.Player,battle.Catalog.Rules,p.Level){Promoted=p.Promoted,Trait=p.Trait};
            new EquipmentLoadout(c,p,battle.Catalog.Equipment,battle.Campaign).Apply(u);ShowUnitDetails(u);Clear(unitDetails);
            FormationText(unitDetails,"캠페인 기술 숙련",new Vector2(.05f,.80f),new Vector2(.95f,.95f),28).color=formationGold;
            FormationText(unitDetails,TacticalDevelopment.MasteryDescription,new Vector2(.05f,.30f),new Vector2(.95f,.77f),23);
            FormationButton(unitDetails,"숙련 안내 닫기","닫기",new Vector2(.65f,.07f),new Vector2(.95f,.22f),()=>CloseUnitDetails());
        }
    }
}
