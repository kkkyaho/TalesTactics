using System;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void DrawFacingArrows()
        {
            foreach(Facing f in Enum.GetValues(typeof(Facing)))
            {
                var facing=f;
                var g=new GameObject(f.ToString(),typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));g.transform.SetParent(commands,false);
                var hit=g.GetComponent<UnityEngine.UI.Image>();hit.color=Color.clear;hit.raycastTarget=true;hit.canvasRenderer.cullTransparentMesh=false;
                var button=g.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=hit;button.transition=UnityEngine.UI.Selectable.Transition.None;
                button.onClick.AddListener(()=>battle.ChooseFacing(facing));
                var hover=g.AddComponent<FacingPointer>();hover.Battle=battle;hover.Direction=facing;
                var art=new GameObject("Overhead arrow",typeof(RectTransform),typeof(FacingArrowGraphic));art.transform.SetParent(g.transform,false);
                var r=(RectTransform)art.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(22,22);
                art.GetComponent<FacingArrowGraphic>().raycastTarget=false;
            }
            message.text="방향 선택 · Enter 확정 · Esc 취소";
            message.rectTransform.sizeDelta=new Vector2(-20,28);message.fontSizeMin=12;message.fontSizeMax=14;
            UpdateFacingArrows();
        }
        void UpdateFacingArrows()
        {
            var unit=battle.Session.Active;var camera=battle.Board.BattleCamera;if(unit==null||camera==null)return;
            float scale=GetComponent<Canvas>().scaleFactor,zoom=battle.Preferences?.TextScale??1;
            var screen=camera.WorldToScreenPoint(battle.Board.HeadWorld(unit));
            var center=new Vector2(screen.x/scale,screen.y/scale+58*zoom);
            float w=Screen.width/scale,h=Screen.height/scale;
            center.x=Mathf.Clamp(center.x,76*zoom,w-76*zoom);center.y=Mathf.Clamp(center.y,68*zoom,h-86-62*zoom);
            var half=new Vector2(72,60)*zoom;Place(commands,Vector2.zero,Vector2.zero,center-half,center+half);
            var origin=camera.WorldToScreenPoint(Vector3.zero);
            foreach(Transform child in commands)
            {
                if(!child.gameObject.activeSelf||!Enum.TryParse<Facing>(child.name,out var facing))continue;
                var d=SkillResolver.FacingVector(facing);var point=camera.WorldToScreenPoint(new Vector3(d.x,0,d.y))-origin;
                var direction=new Vector2(point.x,point.y).normalized;
                var r=(RectTransform)child;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(38,38)*zoom;r.anchoredPosition=direction*44*zoom;
                var arrow=child.GetComponentInChildren<FacingArrowGraphic>();arrow.rectTransform.sizeDelta=new Vector2(22,22)*zoom;
                arrow.SetDirection(direction);
                bool selected=facing==battle.PendingFacing;
                arrow.color=selected?new Color32(247,207,132,255):new Color32(232,207,162,255);
            }
        }
    }
    public sealed class FacingArrowGraphic:UnityEngine.UI.MaskableGraphic
    {
        Vector2 direction=Vector2.up;
        static readonly Vector2[] Shape={new Vector2(0,1),new Vector2(-1,-.08f),new Vector2(-.46f,-.08f),new Vector2(-.46f,-.85f),new Vector2(.46f,-.85f),new Vector2(.46f,-.08f),new Vector2(1,-.08f)};
        public void SetDirection(Vector2 value)
        {if((direction-value).sqrMagnitude<.0001f)return;direction=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            var rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);
            var points=new Vector3[Shape.Length];
            for(int i=0;i<Shape.Length;i++)points[i]=(Vector3)r.center+rotation*new Vector3(Shape[i].x*r.width*.5f,Shape[i].y*r.height*.5f);
            // The short side face follows screen-down, not arrow rotation; it is thickness, not a cast shadow.
            var depth=Vector3.down*(r.height*.11f);
            var side=new Color(color.r*.72f,color.g*.70f,color.b*.66f,color.a);
            for(int i=0;i<points.Length;i++)
            {
                int j=(i+1)%points.Length,n=vh.currentVertCount;
                vh.AddVert(points[i],side,Vector2.zero);vh.AddVert(points[i]+depth,side,Vector2.zero);
                vh.AddVert(points[j]+depth,side,Vector2.zero);vh.AddVert(points[j],side,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
            int top=vh.currentVertCount;foreach(var p in points)vh.AddVert(p,color,Vector2.zero);
            vh.AddTriangle(top,top+1,top+6);vh.AddTriangle(top+2,top+3,top+4);vh.AddTriangle(top+2,top+4,top+5);
        }
    }
}
