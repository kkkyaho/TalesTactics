using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        SkillData[] VisibleSkills=>battle.Session.Active.Data.Skills.Concat(new[]{battle.Session.Active.Data.UltimateSkill})
            .Where(s=>s!=null&&(!battle.TutorialActive||battle.TutorialSkillAllowed(s))).ToArray();
        int skillPage;
        UnitRuntime skillPageOwner;
        SkillData shownSkill;
        float SkillCardHeight=>282;
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
            var selected=(battle.State as SkillDetailsState)?.Skill;
            var skills=VisibleSkills;
            if(skillPageOwner!=unit){skillPageOwner=unit;skillPage=Mathf.Max(0,System.Array.IndexOf(skills,battle.LastSkill))/4;shownSkill=null;}
            if(selected!=null&&selected!=shownSkill){int found=System.Array.IndexOf(skills,selected);if(found>=0)skillPage=found/4;shownSkill=selected;}
            skillPage=Mathf.Clamp(skillPage,0,Mathf.Max(0,(skills.Length-1)/4));
            for(int i=0;i<Mathf.Min(4,skills.Length-skillPage*4);i++)
            {
                var skill=skills[skillPage*4+i];string error=battle.Session.Resolver.CanUse(unit,skill,battle.IsFollowup(skill));
                Button(commands,skill.DisplayName+" · MP"+skill.MPCost+(error!=null?" · 조건 확인":""),0,()=>battle.SetState(new SkillDetailsState(battle,skill)),true,90);
                var r=(RectTransform)commands.GetChild(commands.childCount-1);
                r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(12+i*148,-46);r.sizeDelta=new Vector2(144,98);
                var text=r.GetComponentInChildren<TMP_Text>();text.text=skill.DisplayName;text.fontSize=14;text.enableAutoSizing=true;text.fontSizeMin=11;text.fontSizeMax=14;
                Place(text.rectTransform,Vector2.zero,Vector2.right,new Vector2(3,4),new Vector2(-3,34));
                var cost=Label(r,"MP "+skill.MPCost,4,18,12);cost.alignment=TextAlignmentOptions.TopRight;
                cost.rectTransform.sizeDelta=new Vector2(-10,18);
                bool friendly=skill.Target!=TargetType.Enemy;
                var tint=error!=null?new Color(.5f,.55f,.65f):friendly?new Color(.45f,1,.72f):skill.IsUltimate?new Color(1,.72f,.24f):new Color(.5f,.8f,1);
                string kind=friendly?"heal":skill.IsUltimate?"skill":skill.VisualStyle==CombatVisualStyle.Song?"song":
                    unit.Data.Weapon==WeaponType.Fist||unit.Data.Weapon==WeaponType.Claw?"martial":unit.Data.Weapon==WeaponType.Bow?"bow":
                    unit.Data.Weapon==WeaponType.Gun?"rifle":unit.Data.Weapon==WeaponType.Spear?"spear":unit.Data.Weapon==WeaponType.Shield?"guard":skill.Element!=Element.None?"skill":"attack";
                AddTacticalIcon(r,kind,34,new Vector2(-13,-18),tint);
                if(error!=null){var locked=Label(r,"!",25,24,14);locked.alignment=TextAlignmentOptions.TopRight;locked.color=new Color(1,.7f,.35f);}
                if(selected==skill)
                {r.GetComponent<UnityEngine.UI.Image>().color=new Color(.29f,.23f,.13f);r.GetComponent<UnityEngine.UI.Outline>().effectColor=new Color(1,.78f,.33f);}
                else if(selected==null)RememberedSkillButton(skill);
            }
            Button(commands,"이전 기술 페이지",10,()=>{skillPage--;battle.SetState(new ActionSelectionState(battle));},skillPage>0,28);SkillPageButton(0);
            Button(commands,"다음 기술 페이지",10,()=>{skillPage++;battle.SetState(new ActionSelectionState(battle));},skillPage<(skills.Length-1)/4,28);SkillPageButton(1);
            float y=154;
            if(selected!=null)
            {
                string error=battle.Session.Resolver.CanUse(unit,selected,battle.IsFollowup(selected));
                var brief=Label(commands,$"{selected.DisplayName} · 사거리 {selected.MinRange}–{selected.Range} · {ElementalRules.Name(selected.Element)}",y,24,16);
                var reason=Label(commands,SkillResolver.ExplainUnavailable(error),y+28,26,15);reason.color=error==null?new Color(.5f,1,.7f):new Color(1,.72f,.4f);
                Button(commands,"목표 선택",y+66,()=>battle.SelectSkill(selected),error==null,38);CardFooter(2,3);
                Button(commands,"기술 상세",y+66,()=>ShowSkillInformation(unit,selected),true,38);CardFooter(1,3);
                Button(commands,"스킬 목록으로",y+66,()=>battle.State.Cancel(),true,38);CardFooter(0,3);
            }
            else
            {
                Label(commands,"기술을 선택하면 사용 조건을 확인할 수 있습니다.",y+8,44,16);
                Button(commands,"취소",y+66,()=>battle.SetState(new CommandState(battle)),true,38);CardFooter(0,3);
            }
        }
        void SkillPageButton(int index)
        {
            var r=(RectTransform)commands.GetChild(commands.childCount-1);r.anchorMin=r.anchorMax=Vector2.one;r.pivot=Vector2.one;r.sizeDelta=new Vector2(70,28);r.anchoredPosition=new Vector2(-12-(1-index)*76,-10);
            r.GetComponentInChildren<TMP_Text>().text=index==0?"‹ 이전":"다음 ›";
        }
        void CardFooter(int index,int count)
        {var r=(RectTransform)commands.GetChild(commands.childCount-1);r.anchorMin=new Vector2((float)index/count,1);r.anchorMax=new Vector2((float)(index+1)/count,1);}
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
