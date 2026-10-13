using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        UnitRuntime statusUnit;
        RectTransform statusBackdrop;
        int statusTab,statusPage,statusFacing;
        public bool CharacterStatusOpen=>UnitDetailsOpen&&statusUnit!=null;
        public UnitRuntime DisplayedStatusUnit=>CharacterStatusOpen?statusUnit:null;
        public void ShowUnitDetails(UnitRuntime unit)
        {
            if(unit==null||HelpOpen||SystemMenuOpen||MissionOpen)return;
            PrepareUnitModal(unit);
            statusUnit=unit;statusTab=statusPage=statusFacing=0;
            if(statusBackdrop==null)
            {
                statusBackdrop=Panel("StatusBackdrop",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
                statusBackdrop.GetComponent<Image>().color=new Color(.005f,.015f,.035f,.65f);
                statusBackdrop.GetComponent<Outline>().enabled=false;
            }
            statusBackdrop.gameObject.SetActive(true);statusBackdrop.SetAsLastSibling();unitDetails.SetAsLastSibling();
            RenderCharacterStatus();
        }
        void StatusPortrait(RectTransform parent,CharacterData data)
        {
            int cell=Array.IndexOf(portraitIds,data.Id);var atlas=Resources.Load<Texture2D>("TalesTactics/FormationPortraits");
            if(cell>=0&&atlas!=null)
            {
                var r=FormationRect(parent,"PortraitArt",Vector2.zero,Vector2.one);
                var image=r.gameObject.AddComponent<RawImage>();image.texture=atlas;image.raycastTarget=false;
                image.uvRect=new Rect(cell%5*.2f,cell<5?.5f:0,.2f,.5f);
                var aspect=r.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            }
            else {var image=parent.gameObject.AddComponent<Image>();image.sprite=data.Portrait!=null?data.Portrait:data.Sprites?.Front;image.preserveAspect=true;image.raycastTarget=false;}
        }
        void UpdateStatusLayout()
        {
            if(!CharacterStatusOpen)return;
            float scale=Mathf.Clamp(battle.Preferences.TextScale,.8f,1.5f);
            unitDetails.sizeDelta=new Vector2(Mathf.Min(900*scale,canvas.rect.width-32),Mathf.Min(660*scale,canvas.rect.height-32));
        }
        public void HandleStatusKeyboard()
        {
            if(!CharacterStatusOpen||Keyboard.current==null)return;
            if(Keyboard.current.qKey.wasPressedThisFrame)CycleStatusUnit(-1);
            else if(Keyboard.current.eKey.wasPressedThisFrame)CycleStatusUnit(1);
        }
        void CycleStatusUnit(int direction)
        {
            if(!CharacterStatusOpen)return;
            if(battle.Session!=null)
            {
                var roster=battle.Session.Units.Where(u=>u.Team==statusUnit.Team).ToArray();
                int i=Array.IndexOf(roster,statusUnit);if(i<0||roster.Length<2)return;
                statusUnit=roster[(i+direction+roster.Length)%roster.Length];
            }
            else
            {
                var roster=battle.Catalog.Characters;int i=Array.IndexOf(roster,statusUnit.Data);if(i<0)return;
                var data=roster[(i+direction+roster.Length)%roster.Length];var p=FormationProgress(data);
                var unit=new UnitRuntime(data,Team.Player,battle.Catalog.Rules,battle.TrainingMode?25:p.Level){Promoted=p.Promoted,Trait=p.Trait,Growth=p.Growth};
                new EquipmentLoadout(data,p,battle.Catalog.Equipment,battle.Campaign).Apply(unit);
                unit.CurrentHP=unit.Stats.HP;unit.CurrentMP=unit.Stats.MP;statusUnit=unit;
            }
            statusPage=0;RenderCharacterStatus();
        }
        RectTransform StatusSurface(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var r=FormationRect(parent,name,min,max,4);var img=r.gameObject.AddComponent<Image>();img.color=new Color(.06f,.09f,.15f,.95f);img.raycastTarget=false;
            return r;
        }
        void StatusMeter(Transform parent,string name,int value,int maximum,float y,Color color)
        {
            var r=FormationRect(parent,name+"StatusMeter",new Vector2(.02f,y),new Vector2(.98f,y+.12f));
            FormationText(r,name,Vector2.zero,new Vector2(.18f,1),17).color=formationTeal;
            FormationText(r,value+" / "+maximum,new Vector2(.18f,.35f),Vector2.one,20,TextAlignmentOptions.MidlineRight);
            var bg=StatusSurface(r,"Track",new Vector2(.19f,.05f),new Vector2(1,.3f));
            var fill=FormationRect(bg,"Fill",Vector2.zero,new Vector2(Mathf.Clamp01((float)value/Mathf.Max(1,maximum)),1));
            var image=fill.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;
        }
        void RenderCharacterStatus()
        {
            if(statusUnit==null)return;
            Clear(unitDetails);UpdateStatusLayout();var u=statusUnit;
            unitDetails.GetComponent<Image>().color=new Color(.027f,.045f,.085f,.995f);
            StatusPortrait(FormationRect(unitDetails,"StatusPortrait",new Vector2(.02f,.84f),new Vector2(.14f,.98f)),u.Data);
            FormationText(unitDetails,u.Data.DisplayName,new Vector2(.15f,.9f),new Vector2(.86f,.98f),29);
            FormationText(unitDetails,"Lv "+u.Level+" · "+(u.Promoted?u.Data.PromotionJob:u.Data.Job)+" · "+(u.Team==Team.Player?"아군":"적군"),new Vector2(.15f,.84f),new Vector2(.86f,.9f),18).color=formationGold;
            FormationButton(unitDetails,"닫기","닫기",new Vector2(.87f,.9f),new Vector2(.99f,.98f),()=>CloseUnitDetails());
            string[] tabs={"능력","기술","장비"};
            for(int i=0;i<3;i++){int tab=i;var r=FormationButton(unitDetails,"상태 "+tabs[i],tabs[i],new Vector2(.02f+i*.32f,.765f),new Vector2(.34f+i*.32f,.835f),()=>{statusTab=tab;statusPage=0;RenderCharacterStatus();},true,20);if(tab==statusTab)r.GetComponent<Image>().color=new Color(.04f,.3f,.35f);}
            var model=StatusSurface(unitDetails,"StatusModel",new Vector2(.02f,.15f),new Vector2(.32f,.75f));
            string context=u.Team==Team.Enemy?"전술 · "+EnemyTactics.Describe(u):"특성 · "+TacticalDevelopment.TraitName(u.Trait)+"\n"+TacticalDevelopment.TraitDescription(u.Trait);
            FormationText(model,context,new Vector2(.06f,.7f),new Vector2(.94f,.97f),16).color=formationGold;
            var art=FormationRect(model,"Body",new Vector2(.12f,.2f),new Vector2(.88f,.7f));
            var body=art.gameObject.AddComponent<CharacterModelImage>();body.Character=u.Data;body.sprite=u.Data.Sprites.Get((Facing)statusFacing);body.preserveAspect=true;body.raycastTarget=false;
            FormationButton(model,"모델 회전","모델 회전",new Vector2(.15f,.07f),new Vector2(.85f,.18f),()=>{statusFacing=(statusFacing+1)%4;RenderCharacterStatus();},true,16);
            var content=FormationRect(unitDetails,"StatusContent",new Vector2(.34f,.15f),new Vector2(.98f,.75f));
            if(statusTab==0)DrawStatusAbilities(content,u);
            else if(statusTab==1)DrawStatusSkills(content,u);
            else DrawStatusEquipment(content,u);
            FormationButton(unitDetails,"이전 상태 캐릭터","‹ 이전 · Q",new Vector2(.02f,.025f),new Vector2(.25f,.115f),()=>CycleStatusUnit(-1));
            FormationText(unitDetails,"조회 중 · Esc 닫기",new Vector2(.27f,.03f),new Vector2(.70f,.11f),16,TextAlignmentOptions.Center).color=formationGold;
            FormationButton(unitDetails,"다음 상태 캐릭터","다음 · E ›",new Vector2(.73f,.025f),new Vector2(.98f,.115f),()=>CycleStatusUnit(1));
            // AccessibleText scales glyphs; enlarge the panel without scaling text twice.
        }
        void DrawStatusAbilities(RectTransform parent,UnitRuntime u)
        {
            StatusMeter(parent,"HP",u.CurrentHP,u.Stats.HP,.84f,new Color(.22f,.76f,.52f));
            StatusMeter(parent,"MP",u.CurrentMP,u.Stats.MP,.72f,new Color(.2f,.6f,.88f));
            StatusMeter(parent,"SP",u.SpecialGauge,100,.60f,new Color(.88f,.66f,.28f));
            string[] names={"공격","방어","마력","마방","속도","이동","도약","방향"};
            string[] keys={"STR","DEF","MAG","MDF","SPD","MOV","JMP","Facing"};
            string[] values={u.Stats.STR.ToString(),u.Stats.DEF.ToString(),u.Stats.MAG.ToString(),u.Stats.MDF.ToString(),u.Stats.SPD.ToString(),u.Stats.MOV.ToString(),u.Stats.JMP.ToString(),u.Facing==Facing.Front?"앞":u.Facing==Facing.Back?"뒤":u.Facing==Facing.Left?"왼쪽":"오른쪽"};
            for(int i=0;i<8;i++)
            {
                float x=i%2*.5f,y=.51f-i/2*.075f;
                FormationText(parent,names[i],new Vector2(x,y),new Vector2(x+.24f,y+.07f),18);
                var value=FormationText(parent,values[i],new Vector2(x+.24f,y),new Vector2(x+.49f,y+.07f),20,TextAlignmentOptions.MidlineRight);value.color=formationGold;value.name="StatusValue"+keys[i];
            }
            // A dedicated paginated state card keeps every effect readable even with all debuffs active.
            var states=u.Statuses.Where(s=>s.Turns>0).Select(s=>StatusText.Name(s.Kind)+" · "+(s.Kind==StatusKind.Guard?"다음 자기 턴까지":s.Turns+"턴")).ToList();
            if(!u.Alive)states.Insert(0,"전투 불능 · 부활 필요");
            if(states.Count==0)states.Add("정상 · 적용 중인 상태 없음");
            states.Add("특성 · "+TacticalDevelopment.TraitName(u.Trait));
            if(battle.Session?.Resolver.ReactionsEnabled==true)states.Add("반격 "+(u.CounterUsed?0:1)+" · 지원 "+(u.SupportUsed?0:1));
            int pages=Math.Max(1,(states.Count+1)/2);statusPage=Mathf.Clamp(statusPage,0,pages-1);
            var card=StatusSurface(parent,"StatusEffects",Vector2.zero,new Vector2(1,.265f));
            FormationText(card,"상태 효과",new Vector2(.02f,.65f),new Vector2(.65f,1),18).color=formationTeal;
            FormationText(card,string.Join("\n",states.Skip(statusPage*2).Take(2)),new Vector2(.02f,.03f),new Vector2(.83f,.66f),17);
            FormationButton(card,"상태 효과 다음",(statusPage+1)+"/"+pages+" ›",new Vector2(.83f,.03f),new Vector2(1,.63f),()=>{statusPage=(statusPage+1)%pages;RenderCharacterStatus();},pages>1,14);
        }
        void DrawStatusSkills(RectTransform parent,UnitRuntime u)
        {
            var skills=new[]{u.Data.BasicAttack}.Concat(u.Data.Skills??new SkillData[0]).Concat(u.TacticalSkills??new SkillData[0]).Concat(new[]{u.Data.UltimateSkill}).Where(s=>s!=null).Distinct().ToArray();
            int pages=Math.Max(1,(skills.Length+3)/4);statusPage=Mathf.Clamp(statusPage,0,pages-1);
            for(int i=0;i<4&&statusPage*4+i<skills.Length;i++)
            {
                var s=skills[statusPage*4+i];float top=1-i*.215f;
                var row=StatusSurface(parent,"StatusSkill"+i,new Vector2(0,top-.205f),new Vector2(1,top));
                FormationIcon(row,s.IsUltimate?"skill":FormationWeaponIcon(u.Data),new Vector2(.01f,.2f),new Vector2(.09f,.8f),formationGold);
                FormationText(row,s.DisplayName+(u.Unlocked(s)?"":" · Lv"+s.UnlockLevel+" 해금"),new Vector2(.1f,.5f),new Vector2(.99f,1),20).color=u.Unlocked(s)?formationTeal:Color.gray;
                string cost="MP "+s.MPCost+(s.HPCost>0?" · HP "+s.HPCost:"")+(s.HPPercentCost>0?" · HP "+s.HPPercentCost.ToString("P0"):"")+(s.GaugeCost>0?" · SP "+s.GaugeCost:"");
                FormationText(row,cost+" · "+SkillSummary.Describe(s),new Vector2(.1f,.04f),new Vector2(.99f,.51f),16);
            }
            StatusPages(parent,pages);
        }
        void DrawStatusEquipment(RectTransform parent,UnitRuntime u)
        {
            string[] slots={"무기","방어구","장신구"};
            for(int i=0;i<3;i++)
            {
                var item=u.Equipment[i];float top=1-i*.26f;
                var row=StatusSurface(parent,"StatusEquipment"+i,new Vector2(0,top-.245f),new Vector2(1,top));
                FormationIcon(row,i==0?FormationWeaponIcon(u.Data):i==1?"guard":"accessory",new Vector2(.02f,.25f),new Vector2(.14f,.75f),formationGold);
                FormationText(row,slots[i]+" · "+(item==null?"미장착":item.DisplayName),new Vector2(.17f,.52f),new Vector2(.98f,.96f),20).color=formationTeal;
                FormationText(row,item==null?"장비 효과 없음":EquipmentBonuses(item),new Vector2(.17f,.04f),new Vector2(.98f,.53f),16);
            }
            FormationText(parent,TacticalDevelopment.TraitDescription(u.Trait),new Vector2(0,0),new Vector2(1,.20f),16);
        }
        string EquipmentBonuses(EquipmentData item)
        {
            var s=item.Bonus;string[] n={"HP","MP","공격","방어","마력","마방","속도","이동","도약"};int[] v={s.HP,s.MP,s.STR,s.DEF,s.MAG,s.MDF,s.SPD,s.MOV,s.JMP};
            var parts=n.Select((name,i)=>v[i]==0?null:name+" "+v[i].ToString("+0;-0")).Where(x=>x!=null).ToArray();return parts.Length==0?"능력치 보너스 없음":string.Join(" · ",parts);
        }
        void StatusPages(RectTransform parent,int pages)
        {
            FormationButton(parent,"상태 이전 기술","‹",Vector2.zero,new Vector2(.2f,.12f),()=>{statusPage--;RenderCharacterStatus();},statusPage>0);
            FormationText(parent,(statusPage+1)+" / "+pages,new Vector2(.22f,0),new Vector2(.78f,.12f),16,TextAlignmentOptions.Center);
            FormationButton(parent,"상태 다음 기술","›",new Vector2(.8f,0),new Vector2(1,.12f),()=>{statusPage++;RenderCharacterStatus();},statusPage+1<pages);
        }
    }
}
