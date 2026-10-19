using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        UnitRuntime presentationCaster,presentationTarget;float presentationUntil;
        void FocusPresentation(UnitRuntime caster,Vector2Int aim,float duration)
        {presentationCaster=caster;presentationTarget=battle.Session.Grid[aim]?.Occupant;presentationUntil=Time.time+Mathf.Max(1.1f,duration);}
        public int PresentationLabelPriority(UnitRuntime unit)=>Time.time>presentationUntil?0:unit==presentationTarget?4:unit==presentationCaster?3:0;
        public void ClearPresentationFocus(){presentationCaster=presentationTarget=null;presentationUntil=0;}
    }
}
