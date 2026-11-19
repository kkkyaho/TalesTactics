using System.Linq;
using TMPro;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        MobileUiLayout mobileLayout;
        UnityEngine.UI.Button mobileConfirm;
        bool mobileLayoutActive;
        bool MobileHud=>battle!=null&&battle.UsesTouchControls;
        Rect UiScreenArea=>mobileLayoutActive&&mobileLayout!=null?mobileLayout.SafeArea:new Rect(0,0,Screen.width,Screen.height);

        void UpdateMobileLayout(bool refresh=true)
        {
            bool active=MobileHud;
            if(active!=mobileLayoutActive)
            {
                mobileLayoutActive=active;
                if(active)
                {
                    if(mobileLayout==null)mobileLayout=gameObject.AddComponent<MobileUiLayout>();
                    canvas=mobileLayout.Initialize(GetComponent<RectTransform>());
                }
                else
                {
                    canvas=GetComponent<RectTransform>();mobileLayout.Restore(canvas);
                }
                if(refresh&&battle.Session!=null)Refresh();
            }
            if(active)mobileLayout.RefreshScreen();
            if(mobileConfirm!=null)mobileConfirm.interactable=battle.CanConfirmTouchSelection;
        }

        void UpdateMobileBattleLayout()
        {
            bool targeting=battle.State is TargetSelectionState,facing=battle.State is FacingSelectionState;
            bool open=!(battle.State is BattleEndState)&&!InputModalOpen&&!battle.StoryActive;
            bool commandVisible=facing||SkillPanel||targeting||battle.State is MoveSelectionState||
                battle.State is CommandState&&battle.Session.Active?.Team==Team.Player||battle.TimingActive||battle.State is TutorialCompleteState;
            left.gameObject.SetActive(open&&!targeting);commands.gameObject.SetActive(open&&commandVisible);
            foreach(var part in new[]{header,left,commands})
            {
                var fit=part.GetComponent<FramedContent>();if(fit!=null)fit.enabled=false;
                part.GetComponent<UnityEngine.UI.Outline>().enabled=false;
            }
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(12,-124),new Vector2(-12,-12));
            header.GetComponent<UnityEngine.UI.Image>().enabled=false;header.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            Place(left,Vector2.zero,Vector2.zero,new Vector2(14,14),new Vector2(434,168));
            left.GetComponent<UnityEngine.UI.Image>().enabled=true;left.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var focused=battle.HoveredUnit?.Alive==true?battle.HoveredUnit:battle.Session.Active;
            if(open&&focused!=null){if(focused!=summaryUnit){Clear(left);DrawUnitSummary(focused);}UpdateSummaryValues(focused);}
            commands.GetComponent<UnityEngine.UI.Image>().enabled=!facing;commands.GetComponent<UnityEngine.UI.Image>().raycastTarget=!facing;
            if(facing)UpdateFacingArrows();
            else if(open&&commandVisible)
            {
                float width=SkillPanel?360:targeting?390:280;
                float height=SkillPanel?SkillCardHeight+24:targeting?targetContextHeight+24:
                    battle.State is CommandState?CommandMenuHeight:battle.State is MoveSelectionState?248:340;
                // Thumb controls have a stable home and do not chase a moving actor.
                Place(commands,new Vector2(1,0),new Vector2(1,0),new Vector2(-width-14,14),new Vector2(-14,14+height));
            }
            Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(14,184),new Vector2(-414,278));
            bool saveWarning=!string.IsNullOrEmpty(battle.MobileSaveNotice);
            bool showMessage=open&&(saveWarning||battle.TutorialActive||battle.HasBattleMessage);
            if(message!=null)message.text=saveWarning?battle.MobileSaveNotice:battle.DisplayBattleMessage;
            footer.GetComponent<UnityEngine.UI.Image>().enabled=showMessage;footer.GetComponent<UnityEngine.UI.Outline>().enabled=false;
            var group=footer.GetComponent<CanvasGroup>();if(group==null)group=footer.gameObject.AddComponent<CanvasGroup>();
            group.alpha=showMessage?1:0;group.blocksRaycasts=false;footer.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            if(tacticalToolbar!=null)tacticalToolbar.gameObject.SetActive(false);
        }

        void DrawMobileCameraControls()
        {
            string[] names={"왼쪽 회전","전체 보기","오른쪽 회전","현재 유닛"};
            System.Action[] actions={()=>battle.Board.RotateCamera(-90),()=>battle.Board.ResetCamera(),()=>battle.Board.RotateCamera(90),()=>battle.Board.FocusCurrent()};
            for(int i=0;i<names.Length;i++)
            {
                Button(header,names[i],8,actions[i],!battle.TimingActive,56);
                var r=(RectTransform)header.GetChild(header.childCount-1);
                Place(r,new Vector2(0,1),new Vector2(0,1),new Vector2(i*88,-64),new Vector2(i*88+84,-8));
                var label=r.GetComponentInChildren<TMP_Text>();label.text=new[]{"좌회전","전체","우회전","현재"}[i];label.fontSizeMin=16;label.fontSizeMax=18;
            }
        }

        void DrawMobileGestureHint()
        {
            if(battle.State is FacingSelectionState)
            {
                Button(header,"방향 선택 취소",72,battle.CancelTouchSelection,true,56);
                var r=(RectTransform)header.GetChild(header.childCount-1);
                Place(r,new Vector2(0,1),new Vector2(0,1),new Vector2(0,-128),new Vector2(172,-72));
            }
            else
            {
                var hint=Label(header,"드래그: 맵 이동  ·  두 손가락: 확대/축소",74,32,16);
                Place(hint.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(0,-106),new Vector2(352,-74));
            }
        }

        void MobileActionButton(string name,string label,float y,System.Action action,bool enabled=true)
        {
            Button(commands,name,y,action,enabled,56);
            var r=(RectTransform)commands.GetChild(commands.childCount-1);var text=r.GetComponentInChildren<TMP_Text>();
            text.text=label;text.fontSizeMin=18;text.fontSizeMax=21;
        }

        void DrawMobileMoveSelection()
        {
            Label(commands,"이동 위치 선택",12,34,22);
            moveRisk=Label(commands,"이동할 칸을 터치하세요",52,60,18);
            if(battle.TouchTileSelection.HasValue)
            {
                var p=battle.TouchTileSelection.Value;
                moveRisk.text="도착 "+p+"\n이동 경로를 확인한 뒤 확정하세요";
            }
            MobileActionButton("이동 확정","여기로 이동",116,battle.ConfirmTouchSelection,battle.CanConfirmTouchSelection);
            mobileConfirm=commands.GetChild(commands.childCount-1).GetComponent<UnityEngine.UI.Button>();
            StyleButton(mobileConfirm.transform,HudRole.Primary);
            MobileActionButton("이동 취소","취소",180,battle.CancelTouchSelection);
        }

        void DrawMobileTargetForecast()
        {
            var unit=battle.Session.Active;var skill=battle.SelectedSkill;var forecast=battle.Forecast;
            var name=Label(commands,skill.DisplayName,12,38,23);
            var text=forecast==null?"대상을 터치하면 피해와 범위를 확인할 수 있습니다.":forecast.Rows.Count==0?forecast.Note:
                "대상 "+forecast.TargetCount+"명 · 격파 예상 "+forecast.EnemyKnockouts+"명\n"+
                string.Join("\n",forecast.Rows.Take(2).Select(r=>r.Unit.Data.DisplayName+"  HP "+r.BeforeHP+" → "+r.AfterHP))+
                "\n아군 HP 감소 "+forecast.AllyHealthLoss+" · 반격/지원 "+forecast.HostileReactions+"회";
            var summary=Label(commands,text,58,140,20);summary.name="Mobile forecast summary";
            var costs=new System.Collections.Generic.List<string>();
            int mp=battle.Session.Resolver.MPCost(unit,skill),hp=battle.Session.Resolver.HPCost(unit,skill);
            if(mp>0)costs.Add("MP "+mp);if(hp>0)costs.Add("HP "+hp);if(skill.GaugeCost>0)costs.Add("SP "+skill.GaugeCost);
            Label(commands,"거리 "+skill.MinRange+"–"+skill.Range+"   "+string.Join(" · ",costs),202,34,18);
            MobileActionButton("모바일 전체 예측","전체 예측 · 효과 보기",244,()=>ShowForecast(0),forecast!=null&&forecast.Rows.Count>0);
            MobileActionButton("공격 실행",skill==unit.Data.BasicAttack?"공격 실행":"기술 실행",308,battle.ConfirmTouchSelection,battle.CanConfirmTouchSelection);
            mobileConfirm=commands.GetChild(commands.childCount-1).GetComponent<UnityEngine.UI.Button>();
            StyleButton(mobileConfirm.transform,HudRole.Primary);
            MobileActionButton("예측 취소","취소",372,battle.CancelTouchSelection);
            targetContextHeight=440;
        }
    }
}
