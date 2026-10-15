using UnityEngine;
using UnityEngine.EventSystems;

namespace TalesTactics
{
    // Unity's automatic arrow-key navigation bypasses GamepadPointer.FocusButton.
    // Mouse selection must keep its current scroll position instead of snapping.
    public sealed class FormationRosterFocus:MonoBehaviour,ISelectHandler
    {
        public void OnSelect(BaseEventData eventData)
        {
            if(eventData is AxisEventData)
                GetComponentInParent<FormationRosterScroll>()?.EnsureVisible((RectTransform)transform);
        }
    }
}
