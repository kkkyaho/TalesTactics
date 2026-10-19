using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform contextHintCard;
        TMP_Text contextHintTitle,contextHintBody;
        ContextHintKind pendingContextHint,shownContextHint;
        float contextHintReadyAt;
        public ContextHintKind ActiveContextHint=>ContextHintOpen?shownContextHint:ContextHintKind.None;
        public bool ContextHintOpen=>contextHintCard!=null&&contextHintCard.gameObject.activeSelf;
        public Rect ContextHintScreenBounds=>ContextHintOpen?CombatLabelPlacement.ScreenRect(contextHintCard):default;
        public void HideContextHints()
        {if(contextHintCard!=null)contextHintCard.gameObject.SetActive(false);pendingContextHint=ContextHintKind.None;}
        bool ContextHintsAllowed=>battle!=null&&battle.Session?.Active!=null&&battle.Session.Active.Team==Team.Player&&battle.Session.Result==BattleResult.Ongoing&&!TitleOpen&&!InputModalOpen&&!battle.StoryActive&&!battle.TutorialActive&&!battle.TimingActive&&
            (battle.State is CommandState||battle.State is FacingSelectionState);
        ContextHintKind NextContextHint()
        {
            if(!ContextHintsAllowed)return ContextHintKind.None;
            if(battle.State is FacingSelectionState)
                return battle.HasLearnedContextHint(ContextHintKind.FacingConfirm)?ContextHintKind.None:ContextHintKind.FacingConfirm;
            if(battle.HasBattleMessage)return ContextHintKind.None;
            if(!battle.HasLearnedContextHint(ContextHintKind.MapPan))return ContextHintKind.MapPan;
            if(!battle.HasLearnedContextHint(ContextHintKind.EnemySkills)&&battle.Session.Units.Any(u=>u.Team==Team.Enemy&&u.Alive&&!u.CrossoverReserve))return ContextHintKind.EnemySkills;
            return ContextHintKind.None;
        }
        public void ContextHintLearned(ContextHintKind kind)
        {
            // Also defer the next card when the player already knew an operation and did it unaided.
            contextHintReadyAt=Time.unscaledTime+8;
            if(shownContextHint==kind)HideContextHints();
        }
        public void DismissContextHint()
        {
            if(!ContextHintOpen||!ContextHintsAllowed)return;
            battle.LearnContextHint(shownContextHint);HideContextHints();
        }
        public void UpdateContextHints()
        {
            var next=NextContextHint();
            if(next==ContextHintKind.None){HideContextHints();return;}
            if(pendingContextHint!=next)
            {
                if(contextHintCard!=null)contextHintCard.gameObject.SetActive(false);
                pendingContextHint=next;contextHintReadyAt=Mathf.Max(contextHintReadyAt,Time.unscaledTime+(next==ContextHintKind.FacingConfirm?1:1.5f));
            }
            if(Time.unscaledTime<contextHintReadyAt)return;
            EnsureContextHintCard();
            if(shownContextHint!=next)
            {
                shownContextHint=next;
                switch(next)
                {
                    case ContextHintKind.MapPan:contextHintTitle.text="맵 둘러보기";contextHintBody.text="전장 위에서 마우스 가운데 버튼을 누른 채 드래그하세요.";break;
                    case ContextHintKind.EnemySkills:contextHintTitle.text="적의 기술 확인";contextHintBody.text="명령 대기 중 적을 누른 뒤 ‘기술 보기’로 사거리와 효과를 확인하세요.";break;
                    case ContextHintKind.FacingConfirm:contextHintTitle.text="방향을 고르면 턴 완료";contextHintBody.text="캐릭터 주위의 방향 화살표를 누르세요. 방향키로 고른 뒤 Enter도 가능합니다.";break;
                }
            }
            contextHintCard.gameObject.SetActive(true);LayoutContextHint();
        }
        void EnsureContextHintCard()
        {
            if(contextHintCard!=null)return;
            contextHintCard=Panel("ContextHintCard",Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(610,120));
            contextHintCard.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            contextHintTitle=FormationText(contextHintCard,"",new Vector2(0,.68f),new Vector2(.87f,1),16);contextHintTitle.name="ContextHintTitle";contextHintTitle.color=formationTeal;
            contextHintBody=FormationText(contextHintCard,"",Vector2.zero,new Vector2(.87f,.66f),18);contextHintBody.name="ContextHintBody";
            var close=CloseIcon(contextHintCard,"조작 안내 닫기",DismissContextHint);
            close.anchorMin=close.anchorMax=new Vector2(1,.5f);close.pivot=new Vector2(1,.5f);close.anchoredPosition=Vector2.zero;close.sizeDelta=new Vector2(42,42);
            var button=close.GetComponent<UnityEngine.UI.Button>();button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            // The help text/background must never eat map clicks or middle-button drags.
            foreach(var graphic in contextHintCard.GetComponentsInChildren<UnityEngine.UI.Graphic>())graphic.raycastTarget=graphic.gameObject==close.gameObject;
            contextHintCard.gameObject.SetActive(false);
        }
        void LayoutContextHint()
        {
            float scale=Mathf.Max(.01f,GetComponent<Canvas>().scaleFactor),textScale=battle.Preferences?.TextScale??1;
            var viewport=BattlefieldViewport;
            var area=new Rect(viewport.x*Screen.width/scale,viewport.y*Screen.height/scale,viewport.width*Screen.width/scale,viewport.height*Screen.height/scale);
            float width=Mathf.Min(610*textScale,area.width-24),height=120*textScale;
            float y=area.yMin+12;
            var footerGroup=footer!=null?footer.GetComponent<CanvasGroup>():null;
            if(footer!=null&&footer.gameObject.activeInHierarchy&&footerGroup!=null&&footerGroup.alpha>.01f)
            {
                var corners=new Vector3[4];footer.GetWorldCorners(corners);
                y=Mathf.Max(y,RectTransformUtility.WorldToScreenPoint(null,corners[1]).y/scale+12);
            }
            y=Mathf.Min(y,area.yMax-height-12);
            var low=new Vector2(area.center.x-width*.5f,y);
            Place(contextHintCard,Vector2.zero,Vector2.zero,low,low+new Vector2(width,height));
        }
    }
}