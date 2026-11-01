using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        // Ordinary combat teaches through its controls. Detailed instructions belong to
        // the explicit tutorial/help screen, never a delayed card over the battlefield.
        // Keep this surface for input/persistence callers and older review tooling;
        // learned-operation flags remain compatible with existing preference files.
        public ContextHintKind ActiveContextHint=>ContextHintKind.None;
        public bool ContextHintOpen=>false;
        public Rect ContextHintScreenBounds=>default;
        public void HideContextHints(){}
        public void ContextHintLearned(ContextHintKind kind){}
        public void DismissContextHint(){}
        public void UpdateContextHints(){}
    }
}
