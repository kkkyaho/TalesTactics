using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        SkillData[] VisibleSkills=>battle.Session.Active.Data.Skills.Concat(new[]{battle.Session.Active.Data.UltimateSkill})
            .Where(s=>s!=null&&(!battle.TutorialActive||battle.TutorialSkillAllowed(s))&&battle.Session.Resolver.CanUse(battle.Session.Active,s,battle.IsFollowup(s))==null).ToArray();
        int skillPage;
        UnitRuntime skillPageOwner;
        SkillData shownSkill;
        float SkillCardHeight=222;
        void AddTacticalIcon(Transform parent,string kind,float size,Vector2 position,Color tint)
        {
            var g=new GameObject("Icon",typeof(RectTransform),typeof(TacticalIcon));g.transform.SetParent(parent,false);
            var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.sizeDelta=Vector2.one*size;r.anchoredPosition=position;
            var icon=g.GetComponent<TacticalIcon>();icon.Kind=kind;icon.color=tint;icon.raycastTarget=false;
        }
        void DrawTacticalHeader()
        {
            var title=Label(header,battle.TutorialActive?"입문 연습":battle.Session.ObjectiveDescription,8,26,18);
            title.rectTransform.anchorMax=new Vector2(0,1);title.rectTransform.sizeDelta=new Vector2(310,26);title.rectTransform.anchoredPosition=new Vector2(170,-8);
            var progress=Label(header,battle.TutorialActive?"이동 · 공격 · 회복 · 대기":MissionBriefing.Progress(battle.Session),36,22,14);
            progress.rectTransform.anchorMax=new Vector2(0,1);progress.rectTransform.sizeDelta=new Vector2(310,22);progress.rectTransform.anchoredPosition=new Vector2(170,-36);
            int i=0;
            foreach(var unit in battle.Session.Scheduler.Preview(battle.Session.Units).Take(4))
            {
                var g=new GameObject("NextUnit"+i,typeof(RectTransform),typeof(UnityEngine.UI.Image));g.transform.SetParent(header,false);
                Place((RectTransform)g.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-120+i*48,-56),new Vector2(-80+i*48,-6));
                var img=g.GetComponent<UnityEngine.UI.Image>();img.sprite=unit.Data.Sprites?.Front;img.preserveAspect=true;img.raycastTarget=false;
                var order=Label(g.transform,(++i).ToString(),34,16,12);order.alignment=TextAlignmentOptions.BottomRight;
                var border=g.AddComponent<UnityEngine.UI.Outline>();border.effectColor=unit.Team==Team.Player?new Color(.3f,.75f,1):new Color(1,.35f,.3f);border.effectDistance=new Vector2(1,-1);
            }
        }
        void CompactHeaderEntries()
        {
            string[] names={"좌회전","초기화","우회전","확대 +","축소 −","현재 유닛","도움말","메뉴 · 저장/설정","임무 · 적 정보"};
            string[] labels={"‹","전체","›","+","−","초점","?","설정","임무"};
            for(int i=0;i<names.Length;i++)
            {
                var child=header.Cast<Transform>().FirstOrDefault(t=>t.gameObject.activeSelf&&t.name==names[i]);if(child==null)continue;
                var r=(RectTransform)child;r.sizeDelta=new Vector2(40,40);r.anchoredPosition=new Vector2(-8-(8-i)*44,-12);
                var text=child.GetComponentInChildren<TMP_Text>();text.text=labels[i];text.fontSize=i==0||i==2||i==3||i==4||i==6?24:13;
                text.rectTransform.sizeDelta=new Vector2(-2,36);
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
            float infoY=38+rows*30,buttonsY=infoY+(shownSkill!=null?28:0)+(pages?28:0);
            SkillCardHeight=buttonsY+44;
            Label(commands,"기술",7,24,16);
            var mp=Label(commands,"MP "+unit.CurrentMP,7,24,14);mp.alignment=TextAlignmentOptions.TopRight;
            var brief=Label(commands,"",infoY,26,13);brief.enableAutoSizing=true;brief.fontSizeMin=11;brief.fontSizeMax=13;
            System.Action<SkillData> inspect=skill=>{shownSkill=skill;brief.text=skill==null?"":$"사거리 {skill.MinRange}–{skill.Range} · {ElementalRules.Name(skill.Element)}";};
            inspect(shownSkill);
            if(page.Length==0)Label(commands,"사용 가능한 기술 없음",38,30,15);
            foreach(var skill in page)
            {
                int i=System.Array.IndexOf(page,skill);
                Button(commands,skill.DisplayName+" · MP"+skill.MPCost,38+i*30,()=>battle.SelectSkill(skill),true,28);
                var r=(RectTransform)commands.GetChild(commands.childCount-1);
                var button=r.GetComponent<UnityEngine.UI.Button>();r.GetComponent<UnityEngine.UI.Outline>().enabled=false;
                r.GetComponent<UnityEngine.UI.Image>().color=Color.white;
                var colors=button.colors;colors.normalColor=new Color(.035f,.055f,.11f,0);colors.highlightedColor=colors.selectedColor=new Color(.14f,.3f,.45f);colors.pressedColor=new Color(.25f,.42f,.56f);button.colors=colors;
                var name=r.GetComponentInChildren<TMP_Text>();name.text=skill.DisplayName;name.alignment=TextAlignmentOptions.MidlineLeft;name.enableAutoSizing=true;name.fontSizeMin=11;name.fontSizeMax=15;
                Place(name.rectTransform,Vector2.zero,Vector2.one,new Vector2(6,0),new Vector2(-62,0));
                var cost=Label(r,skill.MPCost+" MP",0,28,13);cost.alignment=TextAlignmentOptions.MidlineRight;Place(cost.rectTransform,Vector2.zero,Vector2.one,new Vector2(6,0),new Vector2(-6,0));
                var trigger=r.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                foreach(var type in new[]{UnityEngine.EventSystems.EventTriggerType.PointerEnter,UnityEngine.EventSystems.EventTriggerType.Select})
                {var entry=new UnityEngine.EventSystems.EventTrigger.Entry{eventID=type};entry.callback.AddListener(_=>inspect(skill));trigger.triggers.Add(entry);}
                if(skill==battle.LastSkill){Canvas.ForceUpdateCanvases();battle.GetComponent<GamepadPointer>()?.FocusButton(button);}
            }
            if(pages)
            {
                Button(commands,"이전 기술 페이지",infoY+28,()=>{skillPage--;battle.SetState(new ActionSelectionState(battle));},skillPage>0,26);HalfButton(commands,0);
                Button(commands,"다음 기술 페이지",infoY+28,()=>{skillPage++;battle.SetState(new ActionSelectionState(battle));},skillPage<(skills.Length-1)/4,26);HalfButton(commands,1);
                commands.GetChild(commands.childCount-2).GetComponentInChildren<TMP_Text>().text="‹ 이전";
                commands.GetChild(commands.childCount-1).GetComponentInChildren<TMP_Text>().text="다음 ›";
            }
            Button(commands,"취소",buttonsY,()=>battle.SetState(new CommandState(battle)),true,28);if(shownSkill!=null)HalfButton(commands,0);
            if(shownSkill!=null){Button(commands,"기술 상세",buttonsY,()=>ShowSkillInformation(unit,shownSkill),true,28);HalfButton(commands,1);}
        }
        void ShowSkillInformation(UnitRuntime unit,SkillData skill)
        {
            ShowUnitDetails(unit);if(!UnitDetailsOpen)return;Clear(unitDetails);
            Label(unitDetails,"기술 상세",16,36,24);
            var detail=Label(unitDetails,battle.Session.Resolver.Describe(unit,skill),64,326,17);
            detail.enableAutoSizing=true;detail.fontSizeMin=14;detail.fontSizeMax=17;
            Button(unitDetails,"닫기",410,()=>CloseUnitDetails());
        }
    }
}
