using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using InputTouchPhase=UnityEngine.InputSystem.TouchPhase;

namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        bool touchControls, touchSequence, touchBlocked, touchDragging, touchPinched;
        int touchId, pinchFirstId, pinchSecondId;
        Vector2 touchStart;
        float pinchDistance, suppressMouseUntil;
        readonly List<RaycastResult> touchHits=new List<RaycastResult>();
        readonly List<Vector2Int> emptyTouchPath=new List<Vector2Int>();
        BattleState touchSelectionState;
        UnitRuntime touchSelectionUnit;

        public bool UsesTouchControls=>Application.isMobilePlatform||touchControls;
        public Vector2Int? TouchTileSelection {get;private set;}
        bool TouchMapAllowed=>Session?.Active!=null&&State!=null&&Hud!=null&&!Hud.TitleOpen&&!Hud.InputModalOpen&&
            !StoryActive&&!TimingActive&&Session.Active.Team==Team.Player&&Session.Result==BattleResult.Ongoing&&
            !(State is ActionExecutionState)&&!(State is BattleEndState)&&!(State is TutorialCompleteState);
        public bool CanConfirmTouchSelection=>TouchMapAllowed&&TouchTileSelection.HasValue&&
            touchSelectionState==State&&touchSelectionUnit==Session.Active&&
            (State is MoveSelectionState?ValidTouchMove(TouchTileSelection.Value):
             State is TargetSelectionState&&Target==TouchTileSelection&&ValidTouchTarget(TouchTileSelection.Value));

        bool ValidTouchMove(Vector2Int tile)=>Session?.Active!=null&&!Session.Active.Moved&&
            TutorialAllows(TutorialStep.Movement)&&(!TutorialActive||tile==TutorialDestination)&&Session.Grid.Path(Session.Active,tile).Count>1;
        bool ValidTouchTarget(Vector2Int tile)=>SelectedSkill!=null&&TutorialTargetAllowed(tile)&&
            Session.Resolver.InRange(Session.Active,SelectedSkill,tile)&&Session.Resolver.Targets(Session.Active,SelectedSkill,tile).Any()&&
            Session.Resolver.CanUse(Session.Active,SelectedSkill,IsFollowup(SelectedSkill))==null;

        public void ConfirmTouchSelection()
        {
            if(!CanConfirmTouchSelection)return;
            var tile=TouchTileSelection.Value;
            TouchTileSelection=null;touchSelectionState=null;touchSelectionUnit=null;
            if(State is TargetSelectionState)Confirm();
            else State.Tile(tile);
        }
        public void CancelTouchSelection()
        {
            if(!TouchMapAllowed)return;
            bool canCancel=State is MoveSelectionState||State is TargetSelectionState||State is FacingSelectionState||
                State is ActionSelectionState||State is SkillDetailsState;
            ResetTouchInput();
            if(canCancel)State.Cancel();
        }
        // A suspended app or a state transition must never turn an old finger release into a new command.
        public void ResetTouchInput()
        {
            ClearTouchPreview();ResetTouchForTransition();
        }
        void ResetTouchForTransition()
        {
            TouchTileSelection=null;touchSelectionState=null;touchSelectionUnit=null;
            if(touchSequence||Touchscreen.current!=null&&Touchscreen.current.touches.Any(t=>t.press.isPressed))
            {touchSequence=true;touchBlocked=true;}
            touchDragging=false;pinchDistance=0;
            Board?.EndCameraDrag();
        }
        void ClearTouchPreview()
        {
            bool selected=TouchTileSelection.HasValue;
            TouchTileSelection=null;touchSelectionState=null;touchSelectionUnit=null;KeyboardTile=null;
            if(State is TargetSelectionState)ClearTargetPreview();
            else if(State is MoveSelectionState&&Session?.Active!=null)
            {Board.ShowPath(emptyTouchPath);if(selected)Hud.Refresh();}
        }
        bool TouchPointOnMap(Vector2 screen)
        {
            if(Board?.BattleCamera==null||Screen.width<=0||Screen.height<=0)return false;
            if(!Board.BattleCamera.pixelRect.Contains(screen)||
                !Hud.BattlefieldViewport.Contains(new Vector2(screen.x/Screen.width,screen.y/Screen.height)))return false;
            if(EventSystem.current==null)return true;
            touchHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},touchHits);
            return touchHits.Count==0;
        }
        void PreviewTouchTile(Vector2 screen)
        {
            if(!TouchMapAllowed||!TouchPointOnMap(screen))return;
            if(!Board.Pick(screen,out var tile)){ClearTouchPreview();return;}
            if(State is MoveSelectionState)
            {
                if(!ValidTouchMove(tile)){ClearTouchPreview();return;}
                TouchTileSelection=tile;touchSelectionState=State;touchSelectionUnit=Session.Active;
                Hud.Refresh();PreviewTile(tile);
            }
            else if(State is TargetSelectionState)
            {
                if(!ValidTouchTarget(tile)){ClearTouchPreview();return;}
                TouchTileSelection=tile;touchSelectionState=State;touchSelectionUnit=Session.Active;
                SelectTarget(tile);
            }
            else if(State is CommandState)State.Tile(tile);
        }
        void EndTouchGesture()
        {
            touchSequence=touchBlocked=touchDragging=touchPinched=false;
            pinchDistance=0;Board?.EndCameraDrag();
        }
        // UI receives its normal InputSystem pointer events. Only contacts starting on the map own this gesture.
        bool UpdateBattleTouchInput()
        {
            var screen=Touchscreen.current;
            TouchControl first=null,second=null,released=null;int count=0;
            if(screen!=null)
            {
                foreach(var touch in screen.touches)
                {
                    if(touch.press.isPressed)
                    {if(count==0)first=touch;else if(count==1)second=touch;count++;}
                    if(touch.touchId.ReadValue()==touchId&&touch.phase.ReadValue()==InputTouchPhase.Ended)released=touch;
                }
            }
            if(count==0)
            {
                if(touchSequence)
                {
                    float threshold=Mathf.Max(12,14*(Hud!=null?Hud.GetComponent<Canvas>().scaleFactor:1));
                    bool tap=!touchBlocked&&!touchDragging&&!touchPinched&&released!=null&&TouchMapAllowed&&
                        (released.position.ReadValue()-touchStart).sqrMagnitude<threshold*threshold;
                    Vector2 point=released!=null?released.position.ReadValue():Vector2.zero;
                    if(released==null&&!touchBlocked)ClearTouchPreview();
                    EndTouchGesture();suppressMouseUntil=Time.unscaledTime+.15f;
                    if(tap)PreviewTouchTile(point);
                    return true;
                }
                if(Time.unscaledTime<suppressMouseUntil)return true;
                var mouse=Mouse.current;
                if(touchControls&&(mouse!=null&&(mouse.delta.ReadValue().sqrMagnitude>1||mouse.leftButton.wasPressedThisFrame||mouse.rightButton.wasPressedThisFrame)||
                    Keyboard.current!=null&&Keyboard.current.anyKey.wasPressedThisFrame))
                {touchControls=false;ClearTouchPreview();}
                return false;
            }
            touchControls=true;suppressMouseUntil=Time.unscaledTime+.15f;
            cameraDragHeld=false;
            if(!touchSequence)
            {
                touchSequence=true;touchId=first.touchId.ReadValue();touchStart=first.position.ReadValue();
                touchBlocked=count>2||first.phase.ReadValue()!=InputTouchPhase.Began||!TouchMapAllowed||!TouchPointOnMap(touchStart)||
                    count==2&&(second.phase.ReadValue()!=InputTouchPhase.Began||!TouchPointOnMap(second.position.ReadValue()));
                touchDragging=touchPinched=false;pinchDistance=0;
                Board?.EndCameraDrag();
            }
            if(!TouchMapAllowed||count>2){touchBlocked=true;Board?.EndCameraDrag();ClearTouchPreview();}
            if(touchBlocked)return true;
            if(count==2)
            {
                // Never inherit a UI contact or a replacement finger halfway through a pinch.
                if(!touchPinched)
                {
                    if(!TouchPointOnMap(first.position.ReadValue())||!TouchPointOnMap(second.position.ReadValue()))
                    {touchBlocked=true;Board.EndCameraDrag();return true;}
                    touchPinched=true;pinchFirstId=first.touchId.ReadValue();pinchSecondId=second.touchId.ReadValue();
                    pinchDistance=Vector2.Distance(first.position.ReadValue(),second.position.ReadValue());
                    Board.EndCameraDrag();ClearTouchPreview();return true;
                }
                if(first.touchId.ReadValue()!=pinchFirstId||second.touchId.ReadValue()!=pinchSecondId)
                {touchBlocked=true;return true;}
                if(!TouchPointOnMap(first.position.ReadValue())||!TouchPointOnMap(second.position.ReadValue()))
                {touchBlocked=true;return true;}
                float distance=Vector2.Distance(first.position.ReadValue(),second.position.ReadValue());
                if(distance>=8&&pinchDistance>=8)Board.ZoomCamera(Mathf.Clamp(pinchDistance/distance,.5f,2f));
                pinchDistance=distance;return true;
            }
            if(touchPinched||first.touchId.ReadValue()!=touchId)
            {touchBlocked=true;Board.EndCameraDrag();return true;}
            Vector2 position=first.position.ReadValue();
            float dragThreshold=Mathf.Max(12,14*Hud.GetComponent<Canvas>().scaleFactor);
            if(!touchDragging&&(position-touchStart).sqrMagnitude>=dragThreshold*dragThreshold)
            {touchDragging=true;ClearTouchPreview();Board.BeginCameraDrag(touchStart);}
            if(touchDragging)
            {
                if(!TouchPointOnMap(position)){touchBlocked=true;Board.EndCameraDrag();}
                else Board.DragCamera(position);
            }
            return true;
        }
    }
}
