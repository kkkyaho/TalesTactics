using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        SkillData[] VisibleSkills=>battle.Session.Active.Data.Skills.Concat(new[]{battle.Session.Active.Data.UltimateSkill})
            .Where(s=>s!=null&&(!battle.TutorialActive||battle.TutorialSkillAllowed(s))&&(showAllSkills||battle.Session.Resolver.CanUse(battle.Session.Active,s,battle.IsFollowup(s))==null)).ToArray();
        bool showAllSkills;
        int skillPage;
        UnitRuntime skillPageOwner;
        SkillData shownSkill;
        UnitRuntime[] lastOrder=new UnitRuntime[0];
        float SkillCardHeight=222;
        void AddTacticalIcon(Transform parent,string kind,float size,Vector2 position,Color tint)
        {
            var g=new GameObject("Icon",typeof(RectTransform),typeof(TacticalIcon));g.transform.SetParent(parent,false);
            var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.sizeDelta=Vector2.one*size;r.anchoredPosition=position;
            var icon=g.GetComponent<TacticalIcon>();icon.Kind=kind;icon.color=tint;icon.raycastTarget=false;
        }
        void DrawTacticalHeader()
        {
            bool arriving=battle.Session.ReinforcementsArrived&&battle.Session.Scheduler is TeamTurnScheduler eventTurn&&eventTurn.Round==3&&eventTurn.Phase==Team.Player;
            var title=Label(header,battle.TutorialActive?"입문 연습":arriving?"적 증원 2명 도착":BriefVictory(battle.Session,battle.Session.CampaignStage,battle.TrainingMode),16,34,18);
            Place(title.rectTransform,new Vector2(0,.15f),new Vector2(.31f,.85f),Vector2.zero,Vector2.zero);title.enableAutoSizing=true;title.fontSizeMin=11;title.fontSizeMax=18;title.textWrappingMode=TMPro.TextWrappingModes.Normal;
            int i=0;
            var team=battle.Session.Scheduler as TeamTurnScheduler;
            var orderNow=team!=null?battle.Session.Units.Where(u=>u.Team==Team.Player).ToArray():battle.Session.Scheduler.Preview(battle.Session.Units).Take(4).ToArray();
            if(team!=null)
            {
                var phase=Label(header,team.Phase==Team.Player?$"아군 턴 {team.Round} · 미완료 {orderNow.Count(team.CanSelect)}명":$"적군 턴 {team.Round}",1,20,12);
                phase.name="PhaseLabel";Place(phase.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-180,-26),new Vector2(180,-2));phase.alignment=TextAlignmentOptions.Center;phase.enableAutoSizing=true;phase.fontSizeMin=10;phase.fontSizeMax=12;
            }
            foreach(var unit in orderNow)
            {
                var g=new GameObject(team!=null?"아군 선택: "+unit.Data.Id:"NextUnit"+i,typeof(RectTransform),typeof(UnityEngine.UI.Image));g.transform.SetParent(header,false);
                float x=-orderNow.Length*26+i*52;
                Place((RectTransform)g.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(x,-88),new Vector2(x+46,-30));
                var img=g.GetComponent<UnityEngine.UI.Image>();img.color=unit==battle.Session.Active?new Color(.08f,.35f,.39f):new Color(.07f,.11f,.17f);img.raycastTarget=true;
                var art=FormationRect(g.transform,"TurnPortrait",new Vector2(.08f,.34f),new Vector2(.92f,.98f));StatusPortrait(art,unit.Data);
                if(team!=null&&!team.CanSelect(unit))foreach(var graphic in art.GetComponentsInChildren<UnityEngine.UI.Graphic>())graphic.color=new Color(.43f,.48f,.53f,.8f);
                var portraitButton=g.AddComponent<UnityEngine.UI.Button>();portraitButton.interactable=team!=null?battle.CanSelectPlayerUnit(unit):battle.Session.Active?.Team==Team.Player&&!(battle.State is ActionExecutionState);
                portraitButton.onClick.AddListener(()=>{if(team!=null)battle.SelectPlayerUnit(unit);else{battle.InspectUnit(unit);ShowUnitDetails(unit);}});
                var hover=g.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                foreach(var type in new[]{UnityEngine.EventSystems.EventTriggerType.PointerEnter,UnityEngine.EventSystems.EventTriggerType.Select,UnityEngine.EventSystems.EventTriggerType.PointerExit,UnityEngine.EventSystems.EventTriggerType.Deselect})
                {var kind=type;var entry=new UnityEngine.EventSystems.EventTrigger.Entry{eventID=type};entry.callback.AddListener(_=>battle.HoveredUnit=kind==UnityEngine.EventSystems.EventTriggerType.PointerEnter||kind==UnityEngine.EventSystems.EventTriggerType.Select?unit:null);hover.triggers.Add(entry);}
                int previous=System.Array.IndexOf(lastOrder,unit),rank=i++;
                var state=FormationRect(g.transform,"ActionAvailability",new Vector2(.06f,.02f),new Vector2(.94f,.33f));
                if(team!=null)
                {
                    if(!unit.Alive)FormationText(state,"KO",Vector2.zero,Vector2.one,10,TextAlignmentOptions.Center);
                    else if(!team.CanSelect(unit))FormationIcon(state,"done",new Vector2(.24f,0),new Vector2(.76f,1),new Color(.6f,.7f,.73f));
                    else {FormationIcon(state,"move",new Vector2(.02f,0),new Vector2(.48f,1),unit.Moved?new Color(.23f,.29f,.34f):formationTeal);FormationIcon(state,"attack",new Vector2(.52f,0),new Vector2(.98f,1),unit.Acted?new Color(.23f,.29f,.34f):formationGold);}
                }
                else FormationText(state,i.ToString()+(previous>=0&&previous!=rank?(previous>rank?"↑":"↓"):""),Vector2.zero,Vector2.one,10,TextAlignmentOptions.Center);
                if(unit==battle.Session.Active)IconSelectionCorners.Add(g.transform);
                var border=g.AddComponent<UnityEngine.UI.Outline>();border.effectColor=unit==battle.Session.Active?formationTeal:new Color(.22f,.31f,.39f);border.effectDistance=new Vector2(1,-1);
            }
            lastOrder=orderNow;
        }
        void CompactHeaderEntries()
        {
            string[] names={"메뉴 · 저장/설정","임무 · 적 정보"};
            string[] labels={"설정","임무"};
            for(int i=0;i<names.Length;i++)
            {
                var child=header.Cast<Transform>().FirstOrDefault(t=>t.gameObject.activeSelf&&t.name==names[i]);if(child==null)continue;
                var r=(RectTransform)child;r.sizeDelta=new Vector2(72,40);r.anchoredPosition=new Vector2(-12-(1-i)*82,-24);
                var text=child.GetComponentInChildren<TMP_Text>();text.text=labels[i];text.fontSize=16;
                Place(text.rectTransform,Vector2.zero,Vector2.one,new Vector2(4,1),new Vector2(-4,-1));StyleButton(child,HudRole.Quiet);
            }
        }
        void DrawSkillCards(UnitRuntime unit)
        {
            var skills=VisibleSkills;
            if(skillPageOwner!=unit){skillPageOwner=unit;skillPage=Mathf.Max(0,System.Array.IndexOf(skills,battle.LastSkill))/4;shownSkill=null;}
            skillPage=Mathf.Clamp(skillPage,0,Mathf.Max(0,(skills.Length-1)/4));
            var page=skills.Skip(skillPage*4).Take(4).ToArray();
            var legacy=(battle.State as SkillDetailsState)?.Skill;
            shownSkill=legacy??(page.Contains(battle.LastSkill)?battle.LastSkill:page.FirstOrDefault());
            int rows=Mathf.Max(1,page.Length);bool pages=skills.Length>4;
            bool roomy=NarrowBattleHud;
            float firstRow=roomy?54:42,rowStep=roomy?66:42,rowHeight=roomy?60:40;
            float briefHeight=roomy?46:26,pageHeight=roomy?34:26,detailHeight=roomy?38:28;
            float infoY=firstRow+rows*rowStep,pageY=infoY+(shownSkill!=null?briefHeight+4:0),buttonsY=pageY+(pages?pageHeight+6:0);
            SkillCardHeight=buttonsY+(shownSkill!=null?detailHeight+16:8);
            Button(commands,showAllSkills?"기술 · 전체 조회":"기술 · 사용 가능",8,()=>{showAllSkills=!showAllSkills;skillPage=0;battle.Hud.Refresh();},true,roomy?36:28);
            var filter=(RectTransform)commands.GetChild(commands.childCount-1);CompactSkillButton(filter);
            Place(filter,new Vector2(0,1),Vector2.one,new Vector2(24,-8-(roomy?36:28)),new Vector2(-112,-8));
            var mp=Label(commands,"MP "+unit.CurrentMP,8,roomy?36:28,roomy?17:14);mp.alignment=TextAlignmentOptions.MidlineRight;
            Place(mp.rectTransform,Vector2.one,Vector2.one,new Vector2(-100,-8-(roomy?36:28)),new Vector2(-24,-8));
            var brief=Label(commands,"",infoY,briefHeight,roomy?17:13);brief.enableAutoSizing=true;brief.fontSizeMin=roomy?15:11;brief.fontSizeMax=roomy?17:13;
            brief.rectTransform.offsetMin=new Vector2(24,brief.rectTransform.offsetMin.y);brief.rectTransform.offsetMax=new Vector2(-24,brief.rectTransform.offsetMax.y);
            System.Action<SkillData> inspect=skill=>{shownSkill=skill;brief.text=skill==null?"":$"거리 {skill.MinRange}–{skill.Range} · "+SkillSummary.Describe(skill);};
            inspect(shownSkill);
            if(page.Length==0)Label(commands,"사용 가능한 기술 없음",firstRow,30,roomy?18:15);
            foreach(var skill in page)
            {
                int i=System.Array.IndexOf(page,skill);
                int mpCost=battle.Session.Resolver.MPCost(unit,skill);
                bool usable=battle.Session.Resolver.CanUse(unit,skill,battle.IsFollowup(skill))==null;
                Button(commands,skill.DisplayName+" · MP"+mpCost,firstRow+i*rowStep,()=>{if(usable)battle.SelectSkill(skill);else ShowSkillInformation(unit,skill);},true,rowHeight);
                var r=(RectTransform)commands.GetChild(commands.childCount-1);
                r.sizeDelta=new Vector2(-40,rowHeight);
                var button=r.GetComponent<UnityEngine.UI.Button>();r.GetComponent<UnityEngine.UI.Outline>().enabled=false;
                r.GetComponent<UnityEngine.UI.Image>().color=Color.white;
                var colors=button.colors;colors.normalColor=new Color(.035f,.055f,.11f,0);colors.highlightedColor=colors.selectedColor=new Color(.14f,.3f,.45f);colors.pressedColor=new Color(.25f,.42f,.56f);button.colors=colors;
                var name=r.GetComponentInChildren<TMP_Text>();name.text=roomy?skill.DisplayName.Split('/')[0].Trim():skill.DisplayName;if(!usable)name.color=new Color(.65f,.69f,.75f);name.alignment=TextAlignmentOptions.MidlineLeft;name.enableAutoSizing=true;name.fontSizeMin=roomy?17:11;name.fontSizeMax=roomy?19:15;
                float subline=roomy?28:17;
                Place(name.rectTransform,Vector2.zero,Vector2.one,new Vector2(44,subline),new Vector2(roomy?-76:-66,-2));
                FormationIcon(r,usable?SkillVisualLanguage.Icon(skill):"locked",new Vector2(0,.5f),new Vector2(0,.5f),SkillVisualLanguage.Tint(skill));
                var icon=(RectTransform)r.GetChild(r.childCount-1);icon.pivot=new Vector2(.5f,.5f);icon.sizeDelta=new Vector2(28,28);icon.anchoredPosition=new Vector2(22,roomy?3:0);
                var range=Label(r,SkillVisualLanguage.Range(skill),0,subline,roomy?16:11);range.name="Skill range";Place(range.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(44,roomy?2:0),new Vector2(-8,subline));
                if(roomy)range.fontSizeMin=16;
                var cost=Label(r,mpCost+" MP",0,23,roomy?16:13);cost.name="Skill cost";cost.alignment=TextAlignmentOptions.MidlineRight;Place(cost.rectTransform,new Vector2(1,0),Vector2.one,new Vector2(roomy?-72:-64,subline),new Vector2(-8,-2));
                var trigger=r.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                foreach(var type in new[]{UnityEngine.EventSystems.EventTriggerType.PointerEnter,UnityEngine.EventSystems.EventTriggerType.Select})
                {var entry=new UnityEngine.EventSystems.EventTrigger.Entry{eventID=type};entry.callback.AddListener(_=>inspect(skill));trigger.triggers.Add(entry);}
                if(skill==battle.LastSkill){Canvas.ForceUpdateCanvases();battle.GetComponent<GamepadPointer>()?.FocusButton(button);}
            }
            if(pages)
            {
                Button(commands,"이전 기술 페이지",pageY,()=>{skillPage--;battle.SetState(new ActionSelectionState(battle));},skillPage>0,pageHeight);HalfButton(commands,0);
                Button(commands,"다음 기술 페이지",pageY,()=>{skillPage++;battle.SetState(new ActionSelectionState(battle));},skillPage<(skills.Length-1)/4,pageHeight);HalfButton(commands,1);
                CompactSkillButton(commands.GetChild(commands.childCount-2));CompactSkillButton(commands.GetChild(commands.childCount-1));
                commands.GetChild(commands.childCount-2).GetComponentInChildren<TMP_Text>().text="‹ 이전";
                commands.GetChild(commands.childCount-1).GetComponentInChildren<TMP_Text>().text="다음 ›";
            }
            if(shownSkill!=null){Button(commands,"기술 상세",buttonsY,()=>ShowSkillInformation(unit,shownSkill),true,detailHeight);}
        }
        void CompactSkillButton(Transform button)
        {var label=button.GetComponentInChildren<TMP_Text>();label.enableAutoSizing=true;label.fontSizeMin=NarrowBattleHud?16:11;label.fontSizeMax=NarrowBattleHud?18:14;Place(label.rectTransform,Vector2.zero,Vector2.one,new Vector2(8,0),new Vector2(-8,0));}
        void ShowSkillInformation(UnitRuntime unit,SkillData skill)
        {
            PrepareUnitModal(unit);if(!UnitDetailsOpen)return;Clear(unitDetails);
            Label(unitDetails,"기술 상세",16,36,24);
            var detail=Label(unitDetails,battle.Session.Resolver.Describe(unit,skill),64,326,17);
            detail.enableAutoSizing=true;detail.fontSizeMin=14;detail.fontSizeMax=17;
            var reason=Label(unitDetails,battle.Session.Resolver.CanUse(unit,skill,battle.IsFollowup(skill))==null?"현재 사용 가능":SkillResolver.ExplainUnavailable(battle.Session.Resolver.CanUse(unit,skill,battle.IsFollowup(skill))),390,20,14);reason.enableAutoSizing=true;reason.fontSizeMin=11;reason.fontSizeMax=14;
            Button(unitDetails,"닫기",410,()=>CloseUnitDetails());
        }
    }
}
