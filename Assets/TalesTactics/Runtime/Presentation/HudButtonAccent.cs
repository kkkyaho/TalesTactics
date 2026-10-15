using UnityEngine;
using UnityEngine.EventSystems;
namespace TalesTactics
{
    public enum HudRole { Secondary, Primary, Tab, Quiet }

    // An independent, non-interactive focus cue also works with pointer-driven gamepads.
    public sealed class HudButtonAccent:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        IconSelectionCorners cue;
        UnityEngine.UI.Button button;
        OrnateButtonGraphic trim;
        TMPro.TMP_Text label;
        TacticalIcon[] icons;
        int iconChildren=-1;
        bool hover,focus,selected;
        public HudRole Role {get;private set;}
        public void Configure(HudRole role,bool active=false)
        {
            Role=role;selected=active;button=GetComponent<UnityEngine.UI.Button>();
            label=GetComponentInChildren<TMPro.TMP_Text>();
            if(trim==null){trim=OrnateButtonGraphic.Create(transform);trim.transform.SetAsFirstSibling();}
            var image=GetComponent<UnityEngine.UI.Image>();
            image.color=role==HudRole.Primary?new Color(.045f,.32f,.37f):active?new Color(.06f,.22f,.28f):role==HudRole.Quiet||role==HudRole.Tab?new Color(.045f,.07f,.11f):new Color(.085f,.12f,.18f);
            var outline=GetComponent<UnityEngine.UI.Outline>();
            if(outline!=null){outline.enabled=role!=HudRole.Quiet&&role!=HudRole.Tab;outline.effectColor=role==HudRole.Primary?new Color(.28f,.76f,.76f,.85f):new Color(.24f,.31f,.4f,.7f);}
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.2f,1.3f,1.35f);colors.selectedColor=new Color(1.15f,1.25f,1.3f);colors.pressedColor=new Color(.75f,.9f,.93f);colors.disabledColor=new Color(.45f,.48f,.54f);colors.fadeDuration=.08f;button.colors=colors;
            if(cue==null)
            {
                cue=IconSelectionCorners.Add(transform);
            }
            Refresh();
        }
        void Refresh()
        {
            if(cue==null)return;bool enabled=button!=null&&button.IsInteractable();bool active=enabled&&(hover||focus||selected||Role==HudRole.Primary);
            cue.gameObject.SetActive(enabled&&(focus||selected));
            if(trim!=null)trim.State(active,!enabled);
            if(label!=null)label.color=!enabled?new Color(.5f,.55f,.6f):new Color(.92f,.95f,.98f);
            if(iconChildren!=transform.childCount){icons=GetComponentsInChildren<TacticalIcon>();iconChildren=transform.childCount;}
            if(icons!=null)foreach(var icon in icons)icon.color=!enabled?new Color(.4f,.43f,.47f):new Color(.80f,.79f,.66f);
        }
        void LateUpdate(){Refresh();}
        public void OnPointerEnter(PointerEventData e){hover=true;Refresh();}
        public void OnPointerExit(PointerEventData e){hover=false;Refresh();}
        public void OnSelect(BaseEventData e){focus=true;Refresh();}
        public void OnDeselect(BaseEventData e){focus=false;Refresh();}
        void OnDisable(){hover=focus=false;}
    }
}
