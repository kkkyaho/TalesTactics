using UnityEngine;

namespace TalesTactics
{
    // Authored laurel corners frame buttons and portraits. The ink mesh never takes input.
    public sealed class OrnateButtonGraphic:UnityEngine.UI.MaskableGraphic
    {
        public bool Selected,Disabled,FrameOnly;
        UnityEngine.UI.Image paintedFrame;
        public bool NestedPortrait=>FrameOnly&&transform.parent!=null&&transform.parent.GetComponent<UnityEngine.UI.RawImage>()!=null&&transform.parent.GetComponentInParent<UnityEngine.UI.Button>()!=null;
        protected override void OnEnable(){base.OnEnable();if(paintedFrame!=null)paintedFrame.enabled=true;}
        protected override void OnDisable(){if(paintedFrame!=null)paintedFrame.enabled=false;base.OnDisable();}
        public void State(bool selected,bool disabled)
        {
            if(Selected==selected&&Disabled==disabled)return;
            Selected=selected;Disabled=disabled;SetVerticesDirty();RefreshArt();
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;if(r.width<=0||r.height<=0||NestedPortrait)return;
            // Do not overlay white highlights: linear-colour blending turns even low alpha
            // white into a grey bevel and obscures the authored navy/teal surface.
            // A dark contour keeps illustrated faces and gold frames readable over both map and UI surfaces.
            Rim(vh,new Rect(r.x+1.3f,r.y+1.3f,r.width-2.6f,r.height-2.6f),SdUiTheme.Ink,2.4f,4);
        }
        static void Rim(UnityEngine.UI.VertexHelper vh,Rect r,Color c,float width,float cut)
        {
            cut=Mathf.Min(cut,Mathf.Min(r.width,r.height)/4);
            var p=new[]{new Vector2(r.x+cut,r.y),new Vector2(r.xMax-cut,r.y),new Vector2(r.xMax,r.y+cut),new Vector2(r.xMax,r.yMax-cut),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.x+cut,r.yMax),new Vector2(r.x,r.yMax-cut),new Vector2(r.x,r.y+cut)};
            for(int i=0;i<8;i++)Line(vh,p[i],p[(i+1)%8],width,c);
        }
        static void Line(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            var d=(b-a).normalized;var normal=new Vector2(-d.y,d.x)*width*.5f;int n=vh.currentVertCount;
            vh.AddVert(a-normal,c,Vector2.zero);vh.AddVert(a+normal,c,Vector2.zero);vh.AddVert(b+normal,c,Vector2.zero);vh.AddVert(b-normal,c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        void LateUpdate()=>RefreshArt();
        void RefreshArt()
        {
            if(paintedFrame==null)return;
            // A portrait inside an already framed clickable card needs only that outer frame.
            paintedFrame.enabled=!NestedPortrait;
            var r=rectTransform.rect;float corner=Mathf.Clamp(Mathf.Min(r.width,r.height)*.14f,4,10);
            paintedFrame.pixelsPerUnitMultiplier=SdUiTheme.SlicePixels(paintedFrame.sprite)/corner;
            paintedFrame.color=Disabled?new Color(.62f,.66f,.70f,.48f):Color.white;
        }
        public static OrnateButtonGraphic Create(Transform parent,bool frameOnly=false)
        {
            var go=new GameObject("Ornate trim",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var graphic=go.AddComponent<OrnateButtonGraphic>();graphic.raycastTarget=false;graphic.FrameOnly=frameOnly;
            var sprite=SdUiTheme.PanelArt;
            if(sprite!=null)
            {
                var frame=new GameObject("Frame",typeof(RectTransform),typeof(UnityEngine.UI.Image));frame.transform.SetParent(go.transform,false);
                var rect=(RectTransform)frame.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                graphic.paintedFrame=frame.GetComponent<UnityEngine.UI.Image>();graphic.paintedFrame.sprite=sprite;graphic.paintedFrame.type=UnityEngine.UI.Image.Type.Sliced;graphic.paintedFrame.fillCenter=false;graphic.paintedFrame.raycastTarget=false;
                graphic.RefreshArt();
            }
            return graphic;
        }
    }
}
