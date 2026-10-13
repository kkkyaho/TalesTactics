using UnityEngine;
using UnityEngine.EventSystems;
namespace TalesTactics
{
    public enum HudRole { Secondary, Primary, Tab, Quiet }

    // An independent, non-interactive focus cue also works with pointer-driven gamepads.
    public sealed class HudButtonAccent:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        UnityEngine.UI.Image cue;
        UnityEngine.UI.Button button;
        bool hover,focus,selected;
        public HudRole Role {get;private set;}
        public void Configure(HudRole role,bool active=false)
        {
            Role=role;selected=active;button=GetComponent<UnityEngine.UI.Button>();
            var image=GetComponent<UnityEngine.UI.Image>();
            image.color=role==HudRole.Primary?new Color(.045f,.32f,.37f):active?new Color(.06f,.22f,.28f):role==HudRole.Quiet||role==HudRole.Tab?new Color(.045f,.07f,.11f):new Color(.085f,.12f,.18f);
            var outline=GetComponent<UnityEngine.UI.Outline>();
            if(outline!=null){outline.enabled=role!=HudRole.Quiet&&role!=HudRole.Tab;outline.effectColor=role==HudRole.Primary?new Color(.28f,.76f,.76f,.85f):new Color(.24f,.31f,.4f,.7f);}
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.2f,1.3f,1.35f);colors.selectedColor=new Color(1.15f,1.25f,1.3f);colors.pressedColor=new Color(.75f,.9f,.93f);colors.disabledColor=new Color(.45f,.48f,.54f);colors.fadeDuration=.08f;button.colors=colors;
            if(cue==null)
            {
                var g=new GameObject("FocusAccent",typeof(RectTransform),typeof(UnityEngine.UI.Image));g.transform.SetParent(transform,false);
                var r=(RectTransform)g.transform;r.anchorMin=Vector2.zero;r.anchorMax=new Vector2(1,0);r.offsetMin=new Vector2(6,1);r.offsetMax=new Vector2(-6,3);
                cue=g.GetComponent<UnityEngine.UI.Image>();cue.raycastTarget=false;
            }
            Refresh();
        }
        void Refresh(){if(cue==null)return;cue.enabled=button!=null&&button.IsInteractable()&&(hover||focus||selected||Role==HudRole.Primary);cue.color=focus?new Color(.95f,.79f,.43f):new Color(.3f,.82f,.8f);}
        public void OnPointerEnter(PointerEventData e){hover=true;Refresh();}
        public void OnPointerExit(PointerEventData e){hover=false;Refresh();}
        public void OnSelect(BaseEventData e){focus=true;Refresh();}
        public void OnDeselect(BaseEventData e){focus=false;Refresh();}
        void OnDisable(){hover=focus=false;}
    }
}
