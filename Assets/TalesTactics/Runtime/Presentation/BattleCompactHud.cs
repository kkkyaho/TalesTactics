using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform unitDetails;
        public bool UnitDetailsOpen=>unitDetails!=null&&unitDetails.gameObject.activeSelf;
        bool SkillPanel=>battle.State is ActionSelectionState||battle.State is SkillDetailsState;
        string BattleTitle=>battle.State is CommandState?"행동 선택":battle.State is ActionSelectionState?"기술 선택":
            battle.State is SkillDetailsState?"기술 정보":battle.State is TargetSelectionState?"대상 선택":
            battle.State is MoveSelectionState?"이동 위치":battle.State is FacingSelectionState?"방향 선택":
            battle.State is ActionExecutionState?"행동 중":battle.State is BattleEndState?"전투 결과":"턴 진행";
        void UpdateBattleLayout()
        {
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(12,-76),new Vector2(-12,-12));
            Place(left,Vector2.zero,Vector2.zero,new Vector2(12,12),new Vector2(356,186));
            Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(368,12),new Vector2(-12,126));
            if(SkillPanel)
                Place(commands,new Vector2(1,0),Vector2.one,new Vector2(-420,198),new Vector2(-12,-88));
            else if(battle.State is TargetSelectionState)
            {
                Place(commands,new Vector2(1,0),new Vector2(1,0),new Vector2(-246,12),new Vector2(-12,186));
                Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(368,12),new Vector2(-258,186));
            }
            else if(battle.State is MoveSelectionState)
                Place(commands,new Vector2(1,0),new Vector2(1,0),new Vector2(-300,138),new Vector2(-12,438));
            else
                Place(commands,new Vector2(1,1),Vector2.one,new Vector2(-246,-464),new Vector2(-12,-88));
            if(CompactLayout)
            {
                Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(12,198),new Vector2(-12,312));
                Place(commands,new Vector2(1,0),new Vector2(1,0),new Vector2(SkillPanel?-420:-246,324),new Vector2(-12,SkillPanel?904:700));
            }
            if(battle.State is CommandState&&!CompactLayout)PlaceCommandBesideUnit();
        }
        Rect BattleViewport(float scale)
        {
            float bottom=(CompactLayout?(SkillPanel?916:712):198)*scale;
            float right=(!CompactLayout&&SkillPanel?432:12)*scale;
            return new Rect(12*scale/Screen.width,bottom/Screen.height,
                Mathf.Max(1,Screen.width-12*scale-right)/Screen.width,
                Mathf.Max(1,Screen.height-bottom-88*scale)/Screen.height);
        }
        void PlaceCommandBesideUnit()
        {
            var unit=battle.Session.Active;if(unit==null||battle.Board.BattleCamera==null)return;
            var p=battle.Board.BattleCamera.WorldToScreenPoint(battle.Session.Grid[unit.Position].WorldPosition(battle.Catalog.Rules.TileHeight));
            float scale=GetComponent<Canvas>().scaleFactor,w=Screen.width/scale,h=Screen.height/scale;
            float x=p.x/scale<w*.5f?p.x/scale-288:p.x/scale+54,y=Mathf.Clamp(p.y/scale+180,574,h-88);
            if(x+234>w-12)x=p.x/scale-288;
            x=Mathf.Clamp(x,12,Mathf.Max(12,w-246));
            Place(commands,Vector2.zero,Vector2.zero,new Vector2(x,y-376),new Vector2(x+234,y));
        }
        void ArrangeCommandMenu()
        {
            int row=0;
            foreach(Transform child in commands)
            {
                if(!child.gameObject.activeSelf||child.GetComponent<UnityEngine.UI.Button>()==null)continue;
                var r=(RectTransform)child;r.anchoredPosition=new Vector2(0,-(48+row++*44));
                var text=child.GetComponentInChildren<TMPro.TMP_Text>();
                if(child.name=="Move / 이동")text.text="이동";
                else if(child.name=="Attack / 공격")text.text="공격";
                else if(child.name=="Skill / 스킬")text.text="기술";
                else if(child.name=="Guard / 가드")text.text="방어";
                else if(child.name=="Undo Move / 이동 취소")text.text="이동 취소";
                else if(child.name=="Wait / 방향 선택")text.text="대기 · 방향";
                else if(child.name=="Restart")text.text="출전 화면";
            }
        }
        void DrawUnitSummary(UnitRuntime u)
        {
            var portrait=new GameObject("Portrait",typeof(RectTransform),typeof(UnityEngine.UI.Image));portrait.transform.SetParent(left,false);
            var pr=portrait.GetComponent<RectTransform>();Place(pr,Vector2.zero,Vector2.zero,new Vector2(10,32),new Vector2(80,156));
            var img=portrait.GetComponent<UnityEngine.UI.Image>();img.sprite=u.Data.Sprites?.Front;img.preserveAspect=true;img.raycastTarget=false;
            var title=Label(left,u.Data.DisplayName+"  Lv"+u.Level,10,28,18);title.rectTransform.offsetMin=new Vector2(86,title.rectTransform.offsetMin.y);
            SummaryBar("HP",u.CurrentHP,u.Stats.HP,48,new Color(.25f,.78f,.49f));
            SummaryBar("MP",u.CurrentMP,u.Stats.MP,78,new Color(.25f,.57f,.95f));
            SummaryBar("SP",u.SpecialGauge,100,108,new Color(.93f,.69f,.28f));
            var status=Label(left,$"이동 {(u.Moved?"완료":"가능")} · 행동 {(u.Acted?"완료":"가능")}",143,24,14);
            status.rectTransform.offsetMax=new Vector2(-90,status.rectTransform.offsetMax.y);
            Button(left,"능력",139,()=>ShowUnitDetails(u),true,28);
            var button=(RectTransform)left.GetChild(left.childCount-1);button.anchorMin=new Vector2(1,1);button.anchorMax=Vector2.one;button.pivot=new Vector2(1,1);button.sizeDelta=new Vector2(66,28);button.anchoredPosition=new Vector2(-10,-139);
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
            string[] names={"좌회전","초기화","우회전","확대 +","축소 −"};
            System.Action[] actions={()=>battle.Board.RotateCamera(-90),()=>battle.Board.ResetCamera(),()=>battle.Board.RotateCamera(90),()=>battle.Board.ZoomCamera(.9f),()=>battle.Board.ZoomCamera(1.1f)};
            for(int i=0;i<names.Length;i++)
            {
                Button(header,names[i],16,actions[i],!battle.TimingActive,32);
                var r=(RectTransform)header.GetChild(header.childCount-1);r.anchorMin=r.anchorMax=Vector2.one;r.pivot=new Vector2(1,1);r.sizeDelta=new Vector2(62,32);r.anchoredPosition=new Vector2(-8-(4-i)*66,-16);
                var label=r.GetComponentInChildren<TMPro.TMP_Text>();label.fontSize=14;label.rectTransform.sizeDelta=new Vector2(-4,29);label.textWrappingMode=TMPro.TextWrappingModes.NoWrap;
            }
        }
        public void ShowUnitDetails(UnitRuntime u)
        {
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
            foreach(var p in new[]{left,commands,header,footer})
            {if(p==null)continue;var group=p.GetComponent<CanvasGroup>();if(group==null)group=p.gameObject.AddComponent<CanvasGroup>();group.interactable=enabled;}
        }
    }
}
