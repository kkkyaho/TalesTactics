using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform unitDetails;

        UnitRuntime summaryUnit, commandAnchorUnit;
        int commandSide=-1,summaryHP=-1,summaryMP=-1,summarySP=-1,summaryLevel=-1;
        float targetContextHeight=124;
        bool NarrowBattleHud=>Screen.width<=1100;
        float BattleDockWidth=>SkillPanel?320:battle.State is TargetSelectionState?340:200;
        float BattleDockTop=>110;
        float CommandMenuHeight=>32+48*(battle.Session.Active?.CanUndoMove==true?6:5);
        public bool UnitDetailsOpen=>unitDetails!=null&&unitDetails.gameObject.activeSelf;
        bool SkillPanel=>battle.State is ActionSelectionState||battle.State is SkillDetailsState;
        string BattleTitle=>battle.State is ActionExecutionState?"행동 중":"턴 진행";
        void HalfButton(Transform parent,int column)
        {var r=(RectTransform)parent.GetChild(parent.childCount-1);r.anchorMin=new Vector2(column*.5f,1);r.anchorMax=new Vector2((column+1)*.5f,1);}
        void UpdateBattleLayout()
        {
            bool facing=battle.State is FacingSelectionState;
            bool targeting=battle.State is TargetSelectionState;
            bool open=!(battle.State is BattleEndState)&&!InputModalOpen&&!battle.StoryActive;
            bool commandVisible=facing||SkillPanel||targeting||battle.State is MoveSelectionState||
                battle.State is CommandState&&battle.Session.Active?.Team==Team.Player||battle.TimingActive||battle.State is TutorialCompleteState;

            left.gameObject.SetActive(open&&!targeting);
            commands.gameObject.SetActive(open&&commandVisible);
            foreach(var part in new[]{header,left,commands})
            {
                var fit=part.GetComponent<FramedContent>();if(fit!=null)fit.enabled=false;
                part.GetComponent<UnityEngine.UI.Outline>().enabled=false;
            }
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(12,-108),new Vector2(-12,-12));
            header.GetComponent<UnityEngine.UI.Image>().enabled=false;
            header.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            // A stable information dock; it never follows the actor or the camera.
            Place(left,Vector2.zero,Vector2.zero,new Vector2(14,14),new Vector2(434,168));
            left.GetComponent<UnityEngine.UI.Image>().enabled=true;
            left.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var focused=battle.HoveredUnit?.Alive==true?battle.HoveredUnit:battle.HealthHoveredUnit?.Alive==true?battle.HealthHoveredUnit:battle.Session.Active;
            if(open&&focused!=null){if(focused!=summaryUnit){Clear(left);DrawUnitSummary(focused);}UpdateSummaryValues(focused);}
            commands.GetComponent<UnityEngine.UI.Image>().enabled=!facing;
            commands.GetComponent<UnityEngine.UI.Image>().raycastTarget=!facing;
            if(facing)UpdateFacingArrows();
            else if(open&&commandVisible)
            {
                float height=SkillPanel?SkillCardHeight+24:targeting?targetContextHeight+24:
                    battle.State is CommandState?CommandMenuHeight:battle.State is MoveSelectionState?152:340;
                if(targeting)
                {
                    // Forecast stays in a fixed information area, leaving target tiles unobstructed.
                    Place(commands,Vector2.zero,Vector2.zero,new Vector2(14,14),new Vector2(14+BattleDockWidth,14+height));
                }
                else PlaceBesideUnits(commands,battle.Session.Active.Position,BattleDockWidth,height);
            }
            if(tacticalToolbar!=null)tacticalToolbar.gameObject.SetActive(false);
            // Only real feedback and explicit tutorial lessons occupy the message area.
            Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(454,14),new Vector2(-14,112));
            bool showMessage=open&&(battle.TutorialActive||battle.HasBattleMessage)&&!string.IsNullOrWhiteSpace(battle.DisplayBattleMessage);
            footer.GetComponent<UnityEngine.UI.Image>().enabled=showMessage;
            footer.GetComponent<UnityEngine.UI.Outline>().enabled=false;
            var group=footer.GetComponent<CanvasGroup>();if(group==null)group=footer.gameObject.AddComponent<CanvasGroup>();
            group.alpha=showMessage?1:0;group.blocksRaycasts=false;
            footer.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
        }
        Rect BattleViewport(float scale)
        {
            // The former full-height left dock no longer reserves a third of the battlefield.
            float edge=14*scale,top=108*scale;
            return new Rect(edge/Screen.width,edge/Screen.height,
                Mathf.Max(1,Screen.width-2*edge)/Screen.width,Mathf.Max(1,Screen.height-top-edge)/Screen.height);
        }
        void PlaceBesideUnits(RectTransform window,Vector2Int position,float width,float height)
        {
            var camera=battle.Board.BattleCamera;if(camera==null)return;
            float scale=Mathf.Max(.01f,GetComponent<Canvas>().scaleFactor),w=Screen.width/scale,h=Screen.height/scale;
            var actor=battle.Session.Active;
            var projected=camera.WorldToScreenPoint(battle.Session.Grid[position].WorldPosition(battle.Catalog.Rules.TileHeight));
            var p=new Vector2(projected.x/scale,projected.y/scale);
            var actorBounds=battle.Board.UnitScreenBounds(actor);
            float leftEdge=actorBounds.xMin/scale-width-18,rightEdge=actorBounds.xMax/scale+18;
            var candidates=new[]{
                new Vector2(leftEdge,p.y+52-height*.5f),new Vector2(rightEdge,p.y+52-height*.5f),
                new Vector2(leftEdge,p.y+145-height),new Vector2(rightEdge,p.y+145-height),
                new Vector2(p.x-width*.5f,p.y+160),new Vector2(p.x-width*.5f,p.y-height-48),
                new Vector2(leftEdge-90,p.y+52-height*.5f),new Vector2(rightEdge+90,p.y+52-height*.5f),
                new Vector2(leftEdge-150,p.y+52-height*.5f),new Vector2(rightEdge+150,p.y+52-height*.5f)};
            if(commandAnchorUnit!=actor){commandSide=-1;commandAnchorUnit=actor;}
            var best=Vector2.zero;float bestScore=float.PositiveInfinity;int side=0;
            for(int i=0;i<candidates.Length;i++)
            {
                var low=new Vector2(Mathf.Clamp(candidates[i].x,14,Mathf.Max(14,w-width-14)),
                    Mathf.Clamp(candidates[i].y,14,Mathf.Max(14,h-height-114)));
                var rect=new Rect(low,new Vector2(width,height));
                float score=Vector2.SqrMagnitude(rect.center-(p+new Vector2(0,60)))*.002f;
                // Prefer the previous side until it obstructs something, avoiding camera-pan jitter.
                if(commandSide>=0&&i!=commandSide)score+=18;
                if(rect.Overlaps(new Rect(8,8,432,168)))score+=100000;
                foreach(var unit in battle.Session.Units.Where(u=>u.Alive))
                {
                    var bounds=battle.Board.UnitScreenBounds(unit);
                    var body=new Rect(bounds.x/scale-8,bounds.y/scale-8,bounds.width/scale+16,bounds.height/scale+16);
                    float overlapWidth=Mathf.Min(rect.xMax,body.xMax)-Mathf.Max(rect.xMin,body.xMin);
                    float overlapHeight=Mathf.Min(rect.yMax,body.yMax)-Mathf.Max(rect.yMin,body.yMin);
                    if(overlapWidth>0&&overlapHeight>0)score+=overlapWidth*overlapHeight*(unit==actor?100:5);
                }
                if(score<bestScore){bestScore=score;best=low;side=i;}
            }
            commandSide=side;
            Place(window,Vector2.zero,Vector2.zero,best,best+new Vector2(width,height));
        }
        void ArrangeCommandMenu()
        {
            string[] names={"Move / 이동","Attack / 공격","Skill / 스킬","Guard / 가드","Wait / 방향 선택","Undo Move / 이동 취소"};
            string[] labels={"이동","공격",battle.Session.Active.Acted&&battle.CanOpenSkills?"연계 기술":"기술","방어","대기","이동 취소"};
            for(int i=0;i<names.Length;i++)
            {
                var child=commands.Cast<Transform>().FirstOrDefault(t=>t.gameObject.activeSelf&&t.name==names[i]);if(child==null)continue;
                var r=(RectTransform)child;
                Place(r,new Vector2(0,1),Vector2.one,new Vector2(14,-16-i*48-44),new Vector2(-14,-16-i*48));
                StyleButton(child,HudRole.Quiet);
                var text=child.GetComponentInChildren<TMPro.TMP_Text>();text.text=labels[i];text.fontSize=text.fontSizeMax=18;text.fontSizeMin=16;text.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
                FormationIcon(child,new[]{"move","attack","skill","guard","wait","undo"}[i],new Vector2(0,.5f),new Vector2(0,.5f),Color.white);
                var icon=(RectTransform)child.GetChild(child.childCount-1);icon.pivot=new Vector2(.5f,.5f);icon.sizeDelta=new Vector2(32,32);icon.anchoredPosition=new Vector2(26,0);
                Place(text.rectTransform,Vector2.zero,Vector2.one,new Vector2(52,0),new Vector2(-8,0));
            }
        }
        void DrawUnitSummary(UnitRuntime u)
        {
            summaryUnit=u;summaryHP=u.CurrentHP;summaryMP=u.CurrentMP;summarySP=u.SpecialGauge;summaryLevel=u.Level;
            var portrait=FormationRect(left,"Portrait",Vector2.zero,Vector2.zero);
            portrait.offsetMin=new Vector2(16,16);portrait.offsetMax=new Vector2(118,138);StatusPortrait(portrait,u.Data);
            var inspect=FormationRect(portrait,"능력",Vector2.zero,Vector2.one);
            var hit=inspect.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=Color.clear;hit.raycastTarget=true;hit.canvasRenderer.cullTransparentMesh=false;
            var button=inspect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=hit;button.onClick.AddListener(()=>ShowUnitDetails(u));

            var title=Label(left,u.Data.DisplayName+"  Lv."+u.Level,14,28,20);title.name="UnitName";
            Place(title.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(132,-42),new Vector2(-16,-14));
            SummaryBar("HP",u.CurrentHP,u.Stats.HP,50,new Color(.18f,.58f,.46f));
            SummaryBar("MP",u.CurrentMP,u.Stats.MP,80,new Color(.19f,.43f,.65f));
            SummaryBar("SP",u.SpecialGauge,100,110,new Color(.58f,.48f,.29f));
        }
        void SummaryBar(string title,int value,int maximum,float y,Color color)
        {
            var caption=Label(left,title,y,24,15);
            Place(caption.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(132,-y-24),new Vector2(-244,-y));
            var bg=new GameObject(title+"Bar",typeof(RectTransform),typeof(UnityEngine.UI.Image));bg.transform.SetParent(left,false);
            Place(bg.GetComponent<RectTransform>(),new Vector2(0,1),Vector2.one,new Vector2(170,-y-24),new Vector2(-16,-y));
            var surface=bg.GetComponent<UnityEngine.UI.Image>();surface.color=Color.white;surface.raycastTarget=false;OrnatePanel.Apply(surface,6);
            var fit=bg.GetComponent<FramedContent>();if(fit!=null)fit.enabled=false;
            var fill=new GameObject("Fill",typeof(RectTransform),typeof(UnityEngine.UI.Image));fill.transform.SetParent(bg.transform,false);
            float ratio=Mathf.Clamp01((float)value/Mathf.Max(1,maximum));
            Place(fill.GetComponent<RectTransform>(),Vector2.zero,new Vector2(ratio,1),new Vector2(7,5),new Vector2(7-14*ratio,-5));
            fill.GetComponent<UnityEngine.UI.Image>().color=color;fill.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            if(ratio<=0)fill.SetActive(false);
            var label=Label(bg.transform,$"{value} / {maximum}",0,24,15);label.name="Value";label.alignment=TMPro.TextAlignmentOptions.MidlineRight;
            Place(label.rectTransform,Vector2.zero,Vector2.one,new Vector2(9,0),new Vector2(-10,0));
        }
        void UpdateSummaryValues(UnitRuntime u)
        {
            if(summaryHP==u.CurrentHP&&summaryMP==u.CurrentMP&&summarySP==u.SpecialGauge&&summaryLevel==u.Level)return;
            summaryHP=u.CurrentHP;summaryMP=u.CurrentMP;summarySP=u.SpecialGauge;summaryLevel=u.Level;
            left.Find("UnitName").GetComponent<TMPro.TMP_Text>().text=u.Data.DisplayName+"  Lv."+u.Level;
            UpdateSummaryBar("HP",u.CurrentHP,u.Stats.HP);UpdateSummaryBar("MP",u.CurrentMP,u.Stats.MP);UpdateSummaryBar("SP",u.SpecialGauge,100);
        }
        void UpdateSummaryBar(string name,int value,int maximum)
        {
            var bar=left.Find(name+"Bar");if(bar==null)return;
            var fill=(RectTransform)bar.Find("Fill");float ratio=Mathf.Clamp01((float)value/Mathf.Max(1,maximum));
            fill.gameObject.SetActive(ratio>0);fill.anchorMax=new Vector2(ratio,1);fill.offsetMax=new Vector2(7-14*ratio,-5);
            bar.Find("Value").GetComponent<TMPro.TMP_Text>().text=$"{value} / {maximum}";
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
