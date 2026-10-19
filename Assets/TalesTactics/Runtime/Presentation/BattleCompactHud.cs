using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform unitDetails;
        RectTransform battleDock;
        float targetContextHeight=124;
        bool NarrowBattleHud=>Screen.width<=1100;
        float BattleDockWidth=>NarrowBattleHud?364:316;
        float BattleDockTop=>124;
        float CommandMenuHeight=>54+46*(!battle.TutorialActive&&battle.Session.Scheduler is TeamTurnScheduler?(battle.Session.Active?.CanUndoMove==true?7:6):battle.Session.Active?.CanUndoMove==true?6:5);
        public bool UnitDetailsOpen=>unitDetails!=null&&unitDetails.gameObject.activeSelf;
        bool SkillPanel=>battle.State is ActionSelectionState||battle.State is SkillDetailsState;
        string BattleTitle=>battle.State is CommandState?"행동 선택":battle.State is ActionSelectionState?"기술 선택":
            battle.State is SkillDetailsState?"기술 정보":battle.State is TargetSelectionState?"대상 선택":
            battle.State is MoveSelectionState?"이동 위치":battle.State is FacingSelectionState?"방향 선택":
            battle.State is ActionExecutionState?"행동 중":battle.State is BattleEndState?"전투 결과":"턴 진행";
        void HalfButton(Transform parent,int column)
        {var r=(RectTransform)parent.GetChild(parent.childCount-1);r.anchorMin=new Vector2(column*.5f,1);r.anchorMax=new Vector2((column+1)*.5f,1);}
        void UpdateBattleLayout()
        {
            bool facing=battle.State is FacingSelectionState;
            bool targeting=battle.State is TargetSelectionState;
            bool open=!(battle.State is BattleEndState)&&!MissionOpen&&!UnitDetailsOpen&&!SystemMenuOpen&&!HelpOpen&&!battle.StoryActive;
            bool summaryVisible=!(targeting&&NarrowBattleHud);
            commands.gameObject.SetActive(open);left.gameObject.SetActive(open&&summaryVisible);
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(12,-112),new Vector2(-12,-12));
            float width=BattleDockWidth,summaryHeight=178,contentTop=BattleDockTop+(summaryVisible?summaryHeight:12);
            float contentHeight=SkillPanel?SkillCardHeight+24:targeting?targetContextHeight:battle.State is CommandState?CommandMenuHeight:battle.State is MoveSelectionState?148:340;
            if(facing)contentHeight=0;
            Place(left,new Vector2(0,1),new Vector2(0,1),new Vector2(12,-BattleDockTop-summaryHeight),new Vector2(12+width,-BattleDockTop));
            Place(commands,new Vector2(0,1),new Vector2(0,1),new Vector2(12,-contentTop-contentHeight),new Vector2(12+width,-contentTop));
            if(battleDock==null)
            {
                battleDock=Panel("BattleCommandDock",new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero);
                battleDock.SetAsFirstSibling();
            }
            float dockHeight=(summaryVisible?summaryHeight:12)+contentHeight+64;
            Place(battleDock,new Vector2(0,1),new Vector2(0,1),new Vector2(12,-BattleDockTop-dockHeight),new Vector2(12+width,-BattleDockTop));
            battleDock.gameObject.SetActive(open);
            // These two retained panels share one backdrop and never drift away from one another.
            foreach(var part in new[]{left,commands})
            {
                part.GetComponent<UnityEngine.UI.Image>().enabled=false;
                part.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
                part.GetComponent<UnityEngine.UI.Outline>().enabled=false;
            }
            if(tacticalToolbar!=null)
                Place(tacticalToolbar,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-BattleDockTop-dockHeight+20),new Vector2(width,-BattleDockTop-dockHeight+58));
            if(!facing)InsetBattleCommands();
            Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(width+36,12),new Vector2(-12,108));
            if(facing){UpdateFacingArrows();Place(footer,new Vector2(1,0),new Vector2(1,0),new Vector2(-372,12),new Vector2(-12,56));}
            // Messages stay available in contextual states; ordinary command selection needs only the objective.
            bool showMessage=open&&(facing||battle.TutorialActive||battle.HasBattleMessage);
            footer.GetComponent<UnityEngine.UI.Image>().enabled=showMessage;
            footer.GetComponent<UnityEngine.UI.Outline>().enabled=showMessage;
            var group=footer.GetComponent<CanvasGroup>();if(group==null)group=footer.gameObject.AddComponent<CanvasGroup>();
            group.alpha=showMessage?1:0;group.blocksRaycasts=false;
            footer.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
        }
        // Commands share a backdrop with Unit rather than their own visible frame.
        // Reserve that backdrop's inner border even for legacy movement/timing labels.
        void InsetBattleCommands()
        {
            float width=commands.rect.width,inset=24;
            foreach(Transform child in commands)
            {
                if(!child.gameObject.activeSelf||FramedContent.IsDecoration(child)||!(child is RectTransform r))continue;
                float low=r.anchorMin.x*width+r.offsetMin.x,high=r.anchorMax.x*width+r.offsetMax.x;
                if(low<inset)r.offsetMin=new Vector2(r.offsetMin.x+inset-low,r.offsetMin.y);
                if(high>width-inset)r.offsetMax=new Vector2(r.offsetMax.x-(high-width+inset),r.offsetMax.y);
            }
        }
        Rect BattleViewport(float scale)
        {
            float side=(BattleDockWidth+36)*scale,bottom=20*scale;
            return new Rect(side/Screen.width,bottom/Screen.height,
                Mathf.Max(1,Screen.width-side-12*scale)/Screen.width,
                Mathf.Max(1,Screen.height-bottom-124*scale)/Screen.height);
        }
        void PlaceCommandBesideUnit()
        {
            var unit=battle.Session.Active;if(unit==null||battle.Board.BattleCamera==null)return;
            PlaceBesideUnits(commands,unit.Position,250,94+46*(!battle.TutorialActive&&battle.Session.Scheduler is TeamTurnScheduler?(unit.CanUndoMove?7:6):unit.CanUndoMove?6:5));
        }
        void PlaceBesideUnits(RectTransform window,Vector2Int position,float width,float height)
        {
            var camera=battle.Board.BattleCamera;
            float scale=GetComponent<Canvas>().scaleFactor,w=Screen.width/scale,h=Screen.height/scale;
            var projected=camera.WorldToScreenPoint(battle.Session.Grid[position].WorldPosition(battle.Catalog.Rules.TileHeight));
            var p=new Vector2(projected.x/scale,projected.y/scale);
            bool skillContext=window==commands&&(SkillPanel||battle.State is TargetSelectionState);
            float gap=skillContext?90:60,sideY=skillContext?p.y+50-height*.5f:p.y-40;
            var candidates=new[]{new Vector2(p.x-width-gap,sideY),new Vector2(p.x+gap,sideY),new Vector2(p.x-width*.5f,p.y+145),new Vector2(p.x-width*.5f,p.y-height-24)};
            var best=Vector2.zero;float bestScore=float.PositiveInfinity;
            foreach(var candidate in candidates)
            {
                var low=new Vector2(Mathf.Clamp(candidate.x,12,w-width-12),Mathf.Clamp(candidate.y,198,Mathf.Max(198,h-height-124)));
                var rect=new Rect(low,new Vector2(width,height));float score=Vector2.SqrMagnitude(rect.center-p)*.000001f;
                if(skillContext&&candidate==candidates[2])score+=1;
                foreach(var unit in battle.Session.Units.Where(u=>u.Alive))
                {
                    var screen=camera.WorldToScreenPoint(battle.Session.Grid[unit.Position].WorldPosition(battle.Catalog.Rules.TileHeight));
                    var body=new Rect(screen.x/scale-34,screen.y/scale-10,68,130);
                    if(rect.Overlaps(body))score+=unit.Position==position?1000:10;
                }
                if(score<bestScore){bestScore=score;best=low;}
            }
            Place(window,Vector2.zero,Vector2.zero,best,best+new Vector2(width,height));
        }
        void ArrangeCommandMenu()
        {
            var commandHeading=Label(commands,"유닛 명령",4,24,15);commandHeading.color=formationGold;commandHeading.rectTransform.offsetMin=new Vector2(24,commandHeading.rectTransform.offsetMin.y);
            string[] names={"Move / 이동","Attack / 공격","Skill / 스킬","Guard / 가드","Wait / 방향 선택","Undo Move / 이동 취소","아군 턴 종료"};
            string[] labels={"이동","공격",battle.Session.Active.Acted&&battle.CanOpenSkills?"연계 기술":"기술","방어","대기","이동 취소","아군 턴 종료"};
            for(int i=0;i<names.Length;i++)
            {
                var child=commands.Cast<Transform>().FirstOrDefault(t=>t.gameObject.activeSelf&&t.name==names[i]);if(child==null)continue;
                var r=(RectTransform)child;
                r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);
                int row=i==6&&!battle.Session.Active.CanUndoMove?5:i;
                r.anchoredPosition=new Vector2(0,-34-row*46);r.sizeDelta=new Vector2(-36,42);
                child.GetComponent<UnityEngine.UI.Outline>().enabled=false;
                child.GetComponent<UnityEngine.UI.Image>().color=Color.white;
                var button=child.GetComponent<UnityEngine.UI.Button>();var colors=button.colors;
                colors.normalColor=new Color(.035f,.055f,.11f,0);colors.highlightedColor=new Color(.14f,.3f,.45f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.25f,.42f,.56f);colors.disabledColor=new Color(.035f,.055f,.11f,0);button.colors=colors;
                var text=child.GetComponentInChildren<TMPro.TMP_Text>();text.text=labels[i];text.fontSize=text.fontSizeMax=18;text.fontSizeMin=16;text.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
                FormationIcon(child,new[]{"move","attack","skill","guard","wait","undo","endturn"}[i],new Vector2(0,.5f),new Vector2(0,.5f),formationGold);
                var icon=(RectTransform)child.GetChild(child.childCount-1);icon.pivot=new Vector2(.5f,.5f);icon.sizeDelta=new Vector2(32,32);icon.anchoredPosition=new Vector2(28,0);
                Place(text.rectTransform,Vector2.zero,Vector2.one,new Vector2(56,0),new Vector2(-12,0));
            }
        }
        void DrawUnitSummary(UnitRuntime u)
        {
            var portrait=FormationRect(left,"Portrait",Vector2.zero,Vector2.zero);
            portrait.offsetMin=new Vector2(24,48);portrait.offsetMax=new Vector2(96,138);StatusPortrait(portrait,u.Data);

            var title=Label(left,u.Data.DisplayName+"  Lv"+u.Level,22,26,18);title.rectTransform.offsetMin=new Vector2(108,title.rectTransform.offsetMin.y);title.rectTransform.offsetMax=new Vector2(-24,title.rectTransform.offsetMax.y);
            SummaryBar("HP",u.CurrentHP,u.Stats.HP,50,new Color(.25f,.78f,.49f));
            SummaryBar("MP",u.CurrentMP,u.Stats.MP,78,new Color(.25f,.57f,.95f));
            SummaryBar("SP",u.SpecialGauge,100,106,new Color(.93f,.69f,.28f));
            var status=Label(left,$"이동 {(u.Moved?"완료":"가능")} · 행동 {(u.Acted?"완료":"가능")}",140,24,14);
            status.rectTransform.offsetMin=new Vector2(26,status.rectTransform.offsetMin.y);
            status.rectTransform.offsetMax=new Vector2(-90,status.rectTransform.offsetMax.y);
            Button(left,"능력",138,()=>ShowUnitDetails(u),true,30);
            var button=(RectTransform)left.GetChild(left.childCount-1);button.anchorMin=new Vector2(1,1);button.anchorMax=Vector2.one;button.pivot=new Vector2(1,1);button.sizeDelta=new Vector2(66,30);button.anchoredPosition=new Vector2(-24,-138);
        }
        void SummaryBar(string title,int value,int maximum,float y,Color color)
        {
            var bg=new GameObject(title+"Bar",typeof(RectTransform),typeof(UnityEngine.UI.Image));bg.transform.SetParent(left,false);
            Place(bg.GetComponent<RectTransform>(),new Vector2(0,1),Vector2.one,new Vector2(108,-y-24),new Vector2(-24,-y));
            bg.GetComponent<UnityEngine.UI.Image>().color=new Color(.07f,.1f,.17f);bg.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var fill=new GameObject("Fill",typeof(RectTransform),typeof(UnityEngine.UI.Image));fill.transform.SetParent(bg.transform,false);
            Place(fill.GetComponent<RectTransform>(),Vector2.zero,new Vector2(Mathf.Clamp01((float)value/Mathf.Max(1,maximum)),1),Vector2.zero,Vector2.zero);
            fill.GetComponent<UnityEngine.UI.Image>().color=color*.55f;fill.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var label=Label(bg.transform,$"{title}   {value} / {maximum}",0,24,16);label.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
        }
        void DrawCameraControls()
        {
            string[] names={"좌회전","초기화","우회전","확대 +","축소 −","현재 유닛"};
            System.Action[] actions={()=>battle.Board.RotateCamera(-90),()=>battle.Board.ResetCamera(),()=>battle.Board.RotateCamera(90),()=>battle.Board.ZoomCamera(.9f),()=>battle.Board.ZoomCamera(1.1f),()=>battle.Board.FocusCurrent()};
            for(int i=0;i<names.Length;i++)
            {
                Button(header,names[i],16,actions[i],!battle.TimingActive,32);
                var r=(RectTransform)header.GetChild(header.childCount-1);r.anchorMin=r.anchorMax=Vector2.one;r.pivot=new Vector2(1,1);r.sizeDelta=new Vector2(62,32);r.anchoredPosition=new Vector2(-8-(5-i)*66,-16);
                var label=r.GetComponentInChildren<TMPro.TMP_Text>();label.fontSize=14;label.rectTransform.sizeDelta=new Vector2(-4,29);label.textWrappingMode=TMPro.TextWrappingModes.NoWrap;
                if(names[i]=="초기화")label.text="전체 보기";
            }
        }
        void PrepareUnitModal(UnitRuntime u)
        {
            if(HelpOpen||SystemMenuOpen||MissionOpen)return;
            if(unitDetails==null)unitDetails=Panel("UnitDetails",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-230,-240),new Vector2(230,240));
            statusUnit=null;if(statusBackdrop!=null)statusBackdrop.gameObject.SetActive(false);
            unitDetails.sizeDelta=new Vector2(460,480);Clear(unitDetails);unitDetails.gameObject.SetActive(true);unitDetails.SetAsLastSibling();SetMainInteraction(false);
            Label(unitDetails,u.Data.DisplayName+" · Lv"+u.Level,16,36,24);
            Label(unitDetails,(u.Promoted?u.Data.PromotionJob:u.Data.Job)+" · "+(u.Team==Team.Player?"아군":"적군"),58,32,18);
            var info=Label(unitDetails,$"HP {u.CurrentHP} / {u.Stats.HP}    MP {u.CurrentMP} / {u.Stats.MP}\nSTR {u.Stats.STR}    MAG {u.Stats.MAG}\nDEF {u.Stats.DEF}    MDF {u.Stats.MDF}\nSPD {u.Stats.SPD}    MOV {u.Stats.MOV}    JMP {u.Stats.JMP}\n방향: {u.Facing}\n상태: "+StatusText.Describe(u)+(battle.Session?.Resolver.ReactionsEnabled==true?"\n반격 "+(u.CounterUsed?0:1)+" · 지원 "+(u.SupportUsed?0:1)+" (자기 턴에 회복)":"")+"\n특성: "+TacticalDevelopment.TraitName(u.Trait)+(u.Team==Team.Enemy?"\n"+EnemyTactics.Describe(u):"\n"+TacticalDevelopment.TraitDescription(u.Trait)),108,290,19);info.enableAutoSizing=true;info.fontSizeMin=15;info.fontSizeMax=19;
            Button(unitDetails,"닫기",410,()=>CloseUnitDetails());
        }
        public bool CloseUnitDetails()
        {
            statusUnit=null;if(statusBackdrop!=null)statusBackdrop.gameObject.SetActive(false);
            bool open=UnitDetailsOpen;if(unitDetails!=null)unitDetails.gameObject.SetActive(false);
            SetMainInteraction(true);return open;
        }
        void SetMainInteraction(bool enabled)
        {
            foreach(var p in new[]{left,commands,header,footer,center,resultPanel})
            {if(p==null)continue;var group=p.GetComponent<CanvasGroup>();if(group==null)group=p.gameObject.AddComponent<CanvasGroup>();group.interactable=enabled&&!HelpOpen&&!SystemMenuOpen&&!MissionOpen&&!battle.StoryActive;}
        }
    }
}
