using System;
using UnityEngine;

namespace TalesTactics
{
    // The roster is a real clipped ScrollRect. Rebuilding a selected card must not
    // move the player back to the top of a long roster.
    [DefaultExecutionOrder(1100)]
    public sealed class FormationRosterScroll : MonoBehaviour
    {
        UnityEngine.UI.ScrollRect scroll;
        UnityEngine.UI.GridLayoutGroup grid;
        RectTransform content,viewport;
        Action<float> remember;
        float initialOffset;
        bool ready;
        public RectTransform Content=>content;
        public RectTransform Viewport=>viewport;
        public float Offset=>content==null?0:Mathf.Max(0,content.anchoredPosition.y);

        public RectTransform Initialize(float offset,Action<float> saveOffset)
        {
            initialOffset=Mathf.Max(0,offset);remember=saveOffset;
            var image=gameObject.AddComponent<UnityEngine.UI.Image>();image.color=Color.clear;
            scroll=gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
            // The scene input module emits delta 6 for one wheel notch: 12 gives
            // a readable 72-unit step while retaining fractional touchpad input.
            scroll.inertia=false;scroll.scrollSensitivity=12;
            viewport=Rect(transform,"Viewport",Vector2.zero,Vector2.one);
            viewport.offsetMax=new Vector2(-16,0);
            var maskImage=viewport.gameObject.AddComponent<UnityEngine.UI.Image>();maskImage.color=Color.white;
            var mask=viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();mask.showMaskGraphic=false;
            content=Rect(viewport,"Content",new Vector2(0,1),Vector2.one);
            content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;content.anchoredPosition=Vector2.zero;
            grid=content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;
            grid.startCorner=UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft;grid.startAxis=UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment=TextAnchor.UpperLeft;grid.spacing=new Vector2(12,12);grid.padding=new RectOffset(4,4,4,4);
            var fitter=content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.content=content;scroll.viewport=viewport;
            var track=Rect(transform,"RosterScrollbar",new Vector2(1,0),Vector2.one);
            track.offsetMin=new Vector2(-10,4);track.offsetMax=new Vector2(0,-4);
            var trackImage=track.gameObject.AddComponent<UnityEngine.UI.Image>();trackImage.color=new Color(.055f,.085f,.12f,.85f);
            var handle=Rect(track,"Handle",Vector2.zero,Vector2.one);handle.offsetMin=new Vector2(2,0);handle.offsetMax=new Vector2(-2,0);
            var handleImage=handle.gameObject.AddComponent<UnityEngine.UI.Image>();handleImage.color=new Color(.31f,.49f,.53f,.9f);
            var bar=track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            bar.handleRect=handle;bar.targetGraphic=handleImage;bar.size=1;bar.value=1;
            scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.onValueChanged.AddListener(_=>
            {
                if(!ready)return;
                remember?.Invoke(Offset);ClearClippedKeyboardFocus();
            });
            return content;
        }

        static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }

        void LateUpdate()=>Fit();
        void Fit()
        {
            if(grid==null||viewport.rect.width<=0||viewport.rect.height<=0)return;
            var size=new Vector2(Mathf.Max(1,(viewport.rect.width-grid.padding.horizontal-2*grid.spacing.x)/3),
                Mathf.Max(248,(viewport.rect.height-grid.padding.vertical-grid.spacing.y)/2));
            if(!ready||(grid.cellSize-size).sqrMagnitude>.01f)
            {
                float offset=ready?Offset:initialOffset;grid.cellSize=size;
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                ready=true;SetOffset(offset);
            }
        }
        void SetOffset(float offset)
        {
            if(content==null)return;
            scroll.StopMovement();content.anchoredPosition=new Vector2(0,Mathf.Clamp(offset,0,Mathf.Max(0,content.rect.height-viewport.rect.height)));
            remember?.Invoke(Offset);
        }
        void OnDisable(){if(ready)remember?.Invoke(Offset);}

        void ClearClippedKeyboardFocus()
        {
            var events=UnityEngine.EventSystems.EventSystem.current;
            var selected=events?.currentSelectedGameObject;
            if(selected==null||!selected.transform.IsChildOf(content)||selected.GetComponent<UnityEngine.UI.Button>()==null)return;
            var rect=(RectTransform)selected.transform;
            var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            // Pointer clipping does not block EventSystem submit. Leave the viewed
            // character and scroll offset alone, but never submit an invisible card.
            if(!RectTransformUtility.RectangleContainsScreenPoint(viewport,point))events.SetSelectedGameObject(null);
        }

        // Keyboard/gamepad focus can reach offscreen rows without inventing a second
        // pagination path. Reveal the whole card, including its deployment button.
        public void EnsureVisible(RectTransform target)
        {
            if(target==null||content==null||!target.IsChildOf(content))return;
            Fit();Transform card=target;
            while(card.parent!=null&&card.parent!=content)card=card.parent;
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,card);
            float offset=Offset;
            if(bounds.max.y>viewport.rect.yMax-4)offset-=bounds.max.y-viewport.rect.yMax+4;
            else if(bounds.min.y<viewport.rect.yMin+4)offset+=viewport.rect.yMin+4-bounds.min.y;
            SetOffset(offset);
        }
    }
}
