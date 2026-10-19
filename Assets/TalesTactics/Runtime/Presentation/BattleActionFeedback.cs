using UnityEngine;
using UnityEngine.Serialization;
namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        [SerializeField,FormerlySerializedAs("Message")]
        string messageValue="출전 인원을 선택하세요 (1–6명).";
        bool actionFeedbackActive;
        float actionFeedbackRemaining;
        public const float ActionFeedbackDuration=3.2f;
        // Ordinary errors/forecasts are persistent until navigation. Only action results expire.
        public string Message
        {
            get=>messageValue;
            set{messageValue=value??"";actionFeedbackActive=false;actionFeedbackRemaining=0;}
        }
        public bool ActionFeedbackVisible=>actionFeedbackActive&&!string.IsNullOrEmpty(messageValue);
        public bool HasBattleMessage=>!string.IsNullOrEmpty(messageValue);
        public string DisplayBattleMessage=>State is FacingSelectionState?"방향 선택 · Enter 확정 · Esc 취소":
            TutorialActive&&!(State is TargetSelectionState&&Target.HasValue)?TutorialInstruction:Message;
        public void ShowActionFeedback(string text)
        {
            Message=text;actionFeedbackActive=!string.IsNullOrEmpty(text);
            actionFeedbackRemaining=ActionFeedbackDuration;Hud?.UpdateBattleMessageText();
        }
        void UpdateActionFeedback()
        {
            if(!actionFeedbackActive)return;
            if(Session==null||Hud==null||Hud.TitleOpen){Message="";Hud?.UpdateBattleMessageText();return;}
            // Reading a modal must not consume the brief opportunity to read the result.
            if(Hud.InputModalOpen||StoryActive||TimingActive)return;
            actionFeedbackRemaining-=Time.unscaledDeltaTime;
            if(actionFeedbackRemaining>0)return;
            Message="";Hud.UpdateBattleMessageText();
        }
        void ClearMessageForState(BattleState next)
        {
            bool navigation=next is MoveSelectionState||next is ActionSelectionState||next is TargetSelectionState||
                next is FacingSelectionState||next is ActionExecutionState||next is BattleEndState;
            bool newCommand=next is CommandState&&!(State is ActionExecutionState)&&!(State is TurnStartState)&&!(State is TurnEndState);
            if(navigation||newCommand||next is TurnStartState&&!actionFeedbackActive)Message="";
        }
    }
    public sealed partial class BattleHud
    {
        // Expiry changes existing text only; rebuilding the HUD would interrupt mouse/gamepad focus.
        public void UpdateBattleMessageText()
        {if(message!=null&&battle!=null)message.text=battle.DisplayBattleMessage;}
    }
}
