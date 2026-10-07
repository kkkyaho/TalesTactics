using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TalesTactics
{
    // A screen pointer covers every existing menu and tile without a second battle command path.
    [DefaultExecutionOrder(-100)]
    public sealed class GamepadPointer : MonoBehaviour
    {
        BattleDirector battle;
        Canvas overlay;
        RectTransform cursor;
        TMP_Text hint;
        GameObject hovered;
        EventSystem eventSystem;
        bool navigation;
        bool active;
        Vector2 position;
        UnityEngine.UI.Outline keyboardOutline;
        Color previousOutlineColor;
        Vector2 previousOutlineDistance;
        readonly List<RaycastResult> hits=new List<RaycastResult>();
        public Vector2 Position=>position;
        public bool Active=>active;

        public void Initialize(BattleDirector owner)
        {
            battle=owner;position=new Vector2(Screen.width,Screen.height)*0.5f;
            var root=new GameObject("Gamepad pointer",typeof(Canvas));root.transform.SetParent(transform,false);
            overlay=root.GetComponent<Canvas>();overlay.renderMode=RenderMode.ScreenSpaceOverlay;overlay.sortingOrder=32760;
            var marker=new GameObject("Cursor",typeof(RectTransform),typeof(Image));marker.transform.SetParent(root.transform,false);
            cursor=marker.GetComponent<RectTransform>();cursor.sizeDelta=new Vector2(15,15);
            cursor.anchorMin=cursor.anchorMax=Vector2.zero;
            var image=marker.GetComponent<Image>();image.color=new Color(1,.85f,.15f,.9f);image.raycastTarget=false;
            var label=new GameObject("Controls",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(root.transform,false);
            hint=label.GetComponent<TextMeshProUGUI>();hint.font=battle.Hud.Font;hint.fontSize=16;hint.raycastTarget=false;
            hint.alignment=TextAlignmentOptions.Bottom;hint.color=Color.yellow;
            var rect=hint.rectTransform;rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(0,24);
            hint.text="왼쪽 스틱: 커서 · 방향키: 메뉴 순환 · A/×: 선택 · B/○: 취소 · LB/RB: 회전 · LT/RT: 대상 · 오른쪽 스틱: 확대 · R3: 초기화";
            overlay.enabled=false;
        }
        void Activate(bool value)
        {
            if(value==active)return;
            active=value;overlay.enabled=value;
            if(value)
            {
                eventSystem=EventSystem.current;
                if(eventSystem!=null){navigation=eventSystem.sendNavigationEvents;eventSystem.sendNavigationEvents=false;}
            }
            else
            {
                Hover(null);
                if(eventSystem!=null)eventSystem.sendNavigationEvents=navigation;
                eventSystem=null;
            }
        }
        void OnDisable(){if(overlay!=null)Activate(false);ClearKeyboardFocus();}
        void ClearKeyboardFocus()
        {
            if(keyboardOutline!=null){keyboardOutline.effectColor=previousOutlineColor;keyboardOutline.effectDistance=previousOutlineDistance;}
            keyboardOutline=null;
        }
        void KeyboardMenus()
        {
            var keyboard=Keyboard.current;
            if(keyboard!=null&&keyboard.anyKey.wasPressedThisFrame)Activate(false);
            bool menu=battle.Session==null||battle.StoryActive||battle.Hud.InputModalOpen||battle.State is BattleEndState||battle.State is ActionSelectionState||battle.State is CommandState;
            if(menu&&keyboard!=null&&keyboard.tabKey.wasPressedThisFrame)
            {
                var events=EventSystem.current;
                var buttons=battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.isActiveAndEnabled&&b.IsInteractable()).ToArray();
                if(events!=null&&buttons.Length>0)
                {
                    int direction=keyboard.shiftKey.isPressed?-1:1;
                    int index=System.Array.FindIndex(buttons,b=>b.gameObject==events.currentSelectedGameObject);
                    index=index<0?(direction>0?0:buttons.Length-1):(index+direction+buttons.Length)%buttons.Length;
                    events.SetSelectedGameObject(buttons[index].gameObject);
                }
            }
            var selected=EventSystem.current?.currentSelectedGameObject;
            var outline=!active&&selected!=null&&selected.activeInHierarchy?selected.GetComponent<UnityEngine.UI.Outline>():null;
            if(outline==keyboardOutline)return;
            ClearKeyboardFocus();keyboardOutline=outline;
            if(outline!=null){previousOutlineColor=outline.effectColor;previousOutlineDistance=outline.effectDistance;outline.effectColor=new Color(1,.85f,.2f);outline.effectDistance=new Vector2(3,-3);}
        }
        void Update()
        {
            if(battle==null||!battle.enabled)return;
            KeyboardMenus();
            var pad=Gamepad.current;
            if(pad==null){Activate(false);return;}
            var stick=pad.leftStick.ReadValue();var right=pad.rightStick.ReadValue();
            bool pressed=pad.buttonSouth.wasPressedThisFrame||pad.buttonEast.wasPressedThisFrame||pad.dpad.ReadValue()!=Vector2.zero||
                pad.leftShoulder.wasPressedThisFrame||pad.rightShoulder.wasPressedThisFrame||pad.rightStickButton.wasPressedThisFrame||pad.leftTrigger.wasPressedThisFrame||pad.rightTrigger.wasPressedThisFrame;
            if(stick.sqrMagnitude>.01f||right.sqrMagnitude>.01f||pressed)Activate(true);
            else if(Mouse.current!=null&&(Mouse.current.delta.ReadValue().sqrMagnitude>1||Mouse.current.leftButton.wasPressedThisFrame))Activate(false);
            if(!active)return;
            MoveTo(position);hint.fontSize=Mathf.Clamp(Screen.width/90f,9,16);
            if(stick.sqrMagnitude>.01f)MoveTo(position+stick*(Screen.height*.85f*(battle.Preferences?.CursorSpeed??1)*Time.unscaledDeltaTime));
            if(pad.dpad.down.wasPressedThisFrame||pad.dpad.right.wasPressedThisFrame)Cycle(1);
            if(pad.dpad.up.wasPressedThisFrame||pad.dpad.left.wasPressedThisFrame)Cycle(-1);
            var target=Hit();Hover(target);
            if(battle.Session!=null&&!battle.StoryActive&&!battle.Hud.InputModalOpen)
            {
                if(pad.leftTrigger.wasPressedThisFrame)battle.CycleTarget(-1);
                if(pad.rightTrigger.wasPressedThisFrame)battle.CycleTarget(1);
                if(pad.leftShoulder.wasPressedThisFrame)battle.Board.RotateCamera(-90);
                if(pad.rightShoulder.wasPressedThisFrame)battle.Board.RotateCamera(90);
                if(!battle.TimingActive&&pad.rightStickButton.wasPressedThisFrame)battle.Board.ResetCamera();
                if(Mathf.Abs(right.y)>.1f)battle.Board.ZoomCamera(Mathf.Exp(-right.y*Time.unscaledDeltaTime));
                if(target==null&&battle.Board.Pick(position,out var tile))battle.PreviewTile(tile);
            }
            if(pad.buttonSouth.wasPressedThisFrame)Submit();
            else if(pad.buttonEast.wasPressedThisFrame)Cancel();
            cursor.anchoredPosition=position;
        }
        public void MoveTo(Vector2 screen)
        {position=new Vector2(Mathf.Clamp(screen.x,0,Screen.width-1),Mathf.Clamp(screen.y,0,Screen.height-1));}
        GameObject Hit()
        {
            var events=EventSystem.current;if(events==null)return null;
            hits.Clear();events.RaycastAll(new PointerEventData(events){position=position},hits);
            return hits.Count==0?null:hits[0].gameObject;
        }
        void Hover(GameObject target)
        {
            if(target==hovered)return;
            var data=new PointerEventData(EventSystem.current){position=position};
            if(hovered!=null)ExecuteEvents.ExecuteHierarchy(hovered,data,ExecuteEvents.pointerExitHandler);
            hovered=target;
            if(hovered!=null)ExecuteEvents.ExecuteHierarchy(hovered,data,ExecuteEvents.pointerEnterHandler);
        }
        public void Submit()
        {
            if(battle.TimingActive){battle.TimingInput();return;}
            var hit=Hit();
            if(hit!=null)
            {
                var data=new PointerEventData(EventSystem.current){position=position,button=PointerEventData.InputButton.Left};
                ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.pointerClickHandler);
                return;
            }
            battle.HandleBattleClick(position);
        }
        public void Cycle(int direction)
        {
            var buttons=battle.Hud.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled&&b.IsInteractable()).ToArray();
            if(buttons.Length==0)return;
            int index=System.Array.FindIndex(buttons,b=>hovered!=null&&hovered.transform.IsChildOf(b.transform));
            index=index<0?(direction>0?0:buttons.Length-1):(index+direction+buttons.Length)%buttons.Length;
            MoveTo(RectTransformUtility.WorldToScreenPoint(null,buttons[index].transform.TransformPoint(((RectTransform)buttons[index].transform).rect.center)));
            Hover(Hit());
        }
        public void FocusButton(Button button)
        {
            if(!active){EventSystem.current?.SetSelectedGameObject(button.gameObject);return;}
            MoveTo(RectTransformUtility.WorldToScreenPoint(null,button.transform.TransformPoint(((RectTransform)button.transform).rect.center)));Hover(Hit());
        }
        public void Cancel()
        {
            if(battle.Hud.CloseMission()||battle.Hud.CloseSystemMenu()||battle.Hud.CloseHelp()||battle.Hud.CloseUnitDetails())return;
            if(battle.TimingActive||battle.StoryActive)return;
            if(battle.Session!=null)
            {
                if(battle.Session.Active.Team==Team.Player&&!(battle.State is ActionExecutionState)&&!(battle.State is BattleEndState))battle.State?.Cancel();
                return;
            }
            var back=battle.Hud.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.IsInteractable()&&
                (b.GetComponentInChildren<TMP_Text>().text.Contains("돌아")||b.GetComponentInChildren<TMP_Text>().text.EndsWith("목록으로")||b.GetComponentInChildren<TMP_Text>().text=="출전 준비로"));
            if(back!=null)back.onClick.Invoke();
        }
    }
}
