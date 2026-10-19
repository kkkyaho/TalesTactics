using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        int learnedContextHints;
        public bool HasLearnedContextHint(ContextHintKind kind)=>((learnedContextHints|(Preferences?.SeenContextHints??0))&(int)kind)==(int)kind;
        // Settings windows can hold a draft from before a successful action. Never lose learned help.
        public void MergeContextHints(PlayerPreferences draft)
        {if(draft!=null)draft.SeenContextHints|=learnedContextHints|(Preferences?.SeenContextHints??0);}
        public void LearnContextHint(ContextHintKind kind)
        {
            if(kind!=ContextHintKind.MapPan&&kind!=ContextHintKind.EnemySkills&&kind!=ContextHintKind.FacingConfirm)return;
            if(Session?.Active==null||Session.Active.Team!=Team.Player||Session.Result!=BattleResult.Ongoing||TutorialActive||StoryActive||TimingActive||State is ActionExecutionState||State is BattleEndState||HasLearnedContextHint(kind))return;
            learnedContextHints|=(int)kind;
            if(Preferences!=null)
            {
                Preferences.SeenContextHints|=learnedContextHints;
                // Only persist the applied preferences; do not apply display, timing, or a campaign save.
                // A failed/protected write still stays learned in memory and is never retried every frame.
                if(preferenceFile!=null&&preferenceFile.Writable)
                    preferenceFile.Save(JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(Preferences)));
            }
            Hud?.ContextHintLearned(kind);
        }
    }
}