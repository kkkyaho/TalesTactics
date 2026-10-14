using UnityEngine;
using UnityEngine.UI;
namespace TalesTactics
{
    // Pixel-sized bevels remain crisp even on compact command buttons. No raycast surface.
    public sealed class OrnateButtonGraphic:MaskableGraphic
    {
        public bool Selected,Disabled,FrameOnly;
        public void State(bool selected,bool disabled){if(Selected==selected&&Disabled==disabled)return;Selected=selected;Disabled=disabled;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Color gold=new Color(.87f,.71f,.39f,Disabled?.3f:.95f),silver=new Color(.48f,.62f,.66f,Disabled?.2f:.75f);
            if(!FrameOnly)Fill(vh,r,Selected?new Color(.075f,.23f,.27f,Disabled?.3f:1):new Color(.018f,.045f,.08f,.96f));
            Rim(vh,r,Selected?new Color(.53f,.76f,.76f):silver,1.1f,6);
            var inside=new Rect(r.x+3,r.y+3,r.width-6,r.height-6);Rim(vh,inside,Selected?new Color(1,.9f,.56f):new Color(.66f,.58f,.4f,.58f),.65f,4);
            // Recessed bottom edge and sunlit top edge, with corner braces.
            Line(vh,new Vector2(r.x+8,r.yMax-2),new Vector2(r.xMax-8,r.yMax-2),.8f,new Color(1,.91f,.69f,Selected?.95f:.4f));
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})
            {
                var p=new Vector2(x<0?r.x+2:r.xMax-2,y<0?r.y+2:r.yMax-2);
                Line(vh,p+new Vector2(-x*8,0),p+new Vector2(0,-y*8),1.6f,gold);
            }
        }
        static void Fill(VertexHelper vh,Rect r,Color c)
        {
            var light=new Color(Mathf.Min(1,c.r*1.18f+.025f),Mathf.Min(1,c.g*1.18f+.025f),Mathf.Min(1,c.b*1.18f+.025f),c.a);
            int n=vh.currentVertCount;vh.AddVert(new Vector3(r.x,r.y),c,Vector2.zero);vh.AddVert(new Vector3(r.x,r.yMax),light,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMax),light,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.y),c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        static void Rim(VertexHelper vh,Rect r,Color c,float width,float cut)
        {
            cut=Mathf.Min(cut,Mathf.Min(r.width,r.height)/4);
            var p=new[]{new Vector2(r.x+cut,r.y),new Vector2(r.xMax-cut,r.y),new Vector2(r.xMax,r.y+cut),new Vector2(r.xMax,r.yMax-cut),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.x+cut,r.yMax),new Vector2(r.x,r.yMax-cut),new Vector2(r.x,r.y+cut)};
            for(int i=0;i<8;i++)Line(vh,p[i],p[(i+1)%8],width,c);
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            var d=(b-a).normalized;var perpendicular=new Vector2(-d.y,d.x)*width*.5f;int n=vh.currentVertCount;
            vh.AddVert(a-perpendicular,c,Vector2.zero);vh.AddVert(a+perpendicular,c,Vector2.zero);vh.AddVert(b+perpendicular,c,Vector2.zero);vh.AddVert(b-perpendicular,c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        public static OrnateButtonGraphic Create(Transform parent,bool frameOnly=false)
        {
            var go=new GameObject("Ornate trim",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var graphic=go.AddComponent<OrnateButtonGraphic>();graphic.raycastTarget=false;graphic.FrameOnly=frameOnly;return graphic;
        }
    }
}
