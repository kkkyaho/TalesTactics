using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform unitDetails;
        float targetContextHeight=124;
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
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(12,-76),new Vector2(-12,-12));
            Place(left,Vector2.zero,Vector2.zero,new Vector2(12,12),new Vector2(344,164));
            Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(356,12),new Vector2(-12,108));
            if(SkillPanel)
            {
                Place(commands,new Vector2(1,0),new Vector2(1,0),new Vector2(-312,12),new Vector2(-12,SkillCardHeight+12));
                Place(footer,Vector2.zero,new Vector2(0,0),new Vector2(12,176),new Vector2(344,286));
            }
            else if(battle.State is TargetSelectionState)
            {
                Place(commands,new Vector2(1,0),new Vector2(1,0),new Vector2(-180,12),new Vector2(-12,124));
                Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(356,12),new Vector2(-192,150));
            }
            else if(battle.State is MoveSelectionState)
                Place(commands,new Vector2(1,0),new Vector2(1,0),new Vector2(-192,12),new Vector2(-12,90));
            else
                Place(commands,new Vector2(1,1),Vector2.one,new Vector2(-246,-464),new Vector2(-12,-88));
            bool facing=battle.State is FacingSelectionState;
            commands.GetComponent<UnityEngine.UI.Image>().enabled=!facing;
            commands.GetComponent<UnityEngine.UI.Image>().raycastTarget=!facing;
            commands.GetComponent<UnityEngine.UI.Outline>().enabled=!facing;
            if(facing)UpdateFacingArrows();
            if(battle.State is CommandState)PlaceCommandBesideUnit();
            if(SkillPanel)PlaceBesideUnits(commands,battle.Session.Active.Position,300,SkillCardHeight);
            if(battle.State is TargetSelectionState)PlaceBesideUnits(commands,battle.Session.Active.Position,300,targetContextHeight);
            // Messages stay available in contextual states; ordinary command selection needs only the objective.
            bool showMessage=battle.TutorialActive||battle.State is ActionExecutionState||battle.State is BattleEndState||(battle.State is CommandState&&battle.Message.StartsWith("Tile "));
            footer.GetComponent<UnityEngine.UI.Image>().enabled=showMessage;
            footer.GetComponent<UnityEngine.UI.Outline>().enabled=showMessage;
            var group=footer.GetComponent<CanvasGroup>();if(group==null)group=footer.gameObject.AddComponent<CanvasGroup>();
            group.alpha=showMessage?1:0;group.blocksRaycasts=showMessage;
            footer.GetComponent<UnityEngine.UI.Image>().raycastTarget=showMessage;
        }
        Rect BattleViewport(float scale)
        {
            float bottom=198*scale;
            return new Rect(12*scale/Screen.width,bottom/Screen.height,
                Mathf.Max(1,Screen.width-24*scale)/Screen.width,
                Mathf.Max(1,Screen.height-bottom-88*scale)/Screen.height);
        }
        void PlaceCommandBesideUnit()
        {
            var unit=battle.Session.Active;if(unit==null||battle.Board.BattleCamera==null)return;
            PlaceBesideUnits(commands,unit.Position,104,12+26*(unit.CanUndoMove?6:5));
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
                var low=new Vector2(Mathf.Clamp(candidate.x,12,w-width-12),Mathf.Clamp(candidate.y,198,h-height-88));
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
            string[] names={"Move / 이동","Attack / 공격","Skill / 스킬","Guard / 가드","Wait / 방향 선택","Undo Move / 이동 취소"};
            string[] labels={"이동","공격","기술","방어","대기","이동 취소"};
            for(int i=0;i<names.Length;i++)
            {
                var child=commands.Cast<Transform>().FirstOrDefault(t=>t.gameObject.activeSelf&&t.name==names[i]);if(child==null)continue;
                var r=(RectTransform)child;
                r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);
                r.anchoredPosition=new Vector2(4,-6-i*26);r.sizeDelta=new Vector2(96,26);
                child.GetComponent<UnityEngine.UI.Outline>().enabled=false;
                child.GetComponent<UnityEngine.UI.Image>().color=Color.white;
                var button=child.GetComponent<UnityEngine.UI.Button>();var colors=button.colors;
                colors.normalColor=new Color(.035f,.055f,.11f,0);colors.highlightedColor=new Color(.14f,.3f,.45f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.25f,.42f,.56f);colors.disabledColor=new Color(.035f,.055f,.11f,0);button.colors=colors;
                var text=child.GetComponentInChildren<TMPro.TMP_Text>();text.text=labels[i];text.fontSize=16;text.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
                Place(text.rectTransform,Vector2.zero,Vector2.one,new Vector2(8,0),new Vector2(-2,0));
            }
        }
        void DrawUnitSummary(UnitRuntime u)
        {
            var portrait=new GameObject("Portrait",typeof(RectTransform),typeof(UnityEngine.UI.Image));portrait.transform.SetParent(left,false);
            var pr=portrait.GetComponent<RectTransform>();Place(pr,Vector2.zero,Vector2.zero,new Vector2(8,22),new Vector2(78,142));
            var img=portrait.GetComponent<UnityEngine.UI.Image>();img.sprite=u.Data.Sprites?.Front;img.preserveAspect=true;img.raycastTarget=false;
            var title=Label(left,u.Data.DisplayName+"  Lv"+u.Level,10,28,18);title.rectTransform.offsetMin=new Vector2(86,title.rectTransform.offsetMin.y);
            SummaryBar("HP",u.CurrentHP,u.Stats.HP,42,new Color(.25f,.78f,.49f));
            SummaryBar("MP",u.CurrentMP,u.Stats.MP,68,new Color(.25f,.57f,.95f));
            SummaryBar("SP",u.SpecialGauge,100,94,new Color(.93f,.69f,.28f));
            var status=Label(left,$"이동 {(u.Moved?"완료":"가능")} · 행동 {(u.Acted?"완료":"가능")}",122,24,12);
            status.rectTransform.offsetMax=new Vector2(-90,status.rectTransform.offsetMax.y);
            Button(left,"능력",118,()=>ShowUnitDetails(u),true,28);
            var button=(RectTransform)left.GetChild(left.childCount-1);button.anchorMin=new Vector2(1,1);button.anchorMax=Vector2.one;button.pivot=new Vector2(1,1);button.sizeDelta=new Vector2(66,28);button.anchoredPosition=new Vector2(-10,-118);
        }
        void SummaryBar(string title,int value,int maximum,float y,Color color)
        {
            var bg=new GameObject(title+"Bar",typeof(RectTransform),typeof(UnityEngine.UI.Image));bg.transform.SetParent(left,false);
            Place(bg.GetComponent<RectTransform>(),new Vector2(0,1),Vector2.one,new Vector2(88,-y-24),new Vector2(-12,-y));
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
        public void ShowUnitDetails(UnitRuntime u)
        {
            if(HelpOpen||SystemMenuOpen||MissionOpen)return;
            if(unitDetails==null)unitDetails=Panel("UnitDetails",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-230,-240),new Vector2(230,240));
            Clear(unitDetails);unitDetails.gameObject.SetActive(true);unitDetails.SetAsLastSibling();SetMainInteraction(false);
            Label(unitDetails,u.Data.DisplayName+" · Lv"+u.Level,16,36,24);
            Label(unitDetails,(u.Promoted?u.Data.PromotionJob:u.Data.Job)+" · "+(u.Team==Team.Player?"아군":"적군"),58,32,18);
            Label(unitDetails,$"HP {u.CurrentHP} / {u.Stats.HP}    MP {u.CurrentMP} / {u.Stats.MP}\n\nSTR {u.Stats.STR}    MAG {u.Stats.MAG}\nDEF {u.Stats.DEF}    MDF {u.Stats.MDF}\nSPD {u.Stats.SPD}    MOV {u.Stats.MOV}    JMP {u.Stats.JMP}\n\n상태: "+(u.Statuses.Count==0?"정상":string.Join(" / ",u.Statuses.Select(s=>s.Kind+" "+s.Turns))),108,265,20);
            Button(unitDetails,"닫기",410,()=>CloseUnitDetails());
        }
        public bool CloseUnitDetails()
        {
            bool open=UnitDetailsOpen;if(unitDetails!=null)unitDetails.gameObject.SetActive(false);
            SetMainInteraction(true);return open;
        }
        void SetMainInteraction(bool enabled)
        {
            foreach(var p in new[]{left,commands,header,footer,center,resultPanel})
            {if(p==null)continue;var group=p.GetComponent<CanvasGroup>();if(group==null)group=p.gameObject.AddComponent<CanvasGroup>();group.interactable=enabled&&!HelpOpen&&!SystemMenuOpen&&!MissionOpen;}
        }
    }
}
