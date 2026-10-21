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
        UnityEngine.UI.Image surface;
        OrnateButtonGraphic trim;
        TMPro.TMP_Text label;
        TacticalIcon[] icons;
        int iconChildren=-1;
        bool hover,focus,selected;
        public HudRole Role {get;private set;}
        public void Configure(HudRole role,bool active=false)
        {
            Role=role;selected=active;button=GetComponent<UnityEngine.UI.Button>();surface=GetComponent<UnityEngine.UI.Image>();
            label=GetComponentInChildren<TMPro.TMP_Text>();
            if(trim==null){trim=OrnateButtonGraphic.Create(transform);trim.transform.SetAsFirstSibling();}
            var outline=GetComponent<UnityEngine.UI.Outline>();if(outline!=null)outline.enabled=false;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;colors.selectedColor=Color.white;colors.pressedColor=new Color(.78f,.90f,.94f);colors.disabledColor=Color.white;colors.fadeDuration=.08f;button.colors=colors;
            if(cue==null)cue=IconSelectionCorners.Add(transform);
            Refresh();
        }
        void Refresh()
        {
            if(cue==null)return;bool enabled=button!=null&&button.IsInteractable();bool active=enabled&&(hover||focus||selected||Role==HudRole.Primary);
            cue.gameObject.SetActive(enabled&&(focus||selected));
            if(trim!=null)trim.State(active,!enabled);
            if(surface!=null)surface.color=SdUiTheme.Surface(Role,active,!enabled);
            if(label!=null)label.color=enabled?SdUiTheme.Text:SdUiTheme.Muted;
            if(iconChildren!=transform.childCount){icons=GetComponentsInChildren<TacticalIcon>();iconChildren=transform.childCount;}
            if(icons!=null)foreach(var icon in icons)icon.color=enabled?Color.white:new Color(.48f,.51f,.55f);
        }
        void LateUpdate(){Refresh();}
        public void OnPointerEnter(PointerEventData e){hover=true;Refresh();}
        public void OnPointerExit(PointerEventData e){hover=false;Refresh();}
        public void OnSelect(BaseEventData e){focus=true;Refresh();}
        public void OnDeselect(BaseEventData e){focus=false;Refresh();}
        void OnDisable(){hover=focus=false;}
    }
}
