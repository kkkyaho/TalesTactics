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
        public override Texture mainTexture=>IllustratedIcons.Get("direction",true).Texture;
        public void SetDirection(Vector2 value)
        {if((direction-value).sqrMagnitude<.0001f)return;direction=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var region=IllustratedIcons.Get("direction",true);var uv=region.UV;
            var rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);
            float w=region.width/(float)region.height,h=1;
            float boundX=Mathf.Abs(direction.y)*w+Mathf.Abs(direction.x)*h,boundY=Mathf.Abs(direction.x)*w+Mathf.Abs(direction.y)*h;
            float scale=Mathf.Min(r.width/boundX,r.height/boundY)*.5f;
            float brightness=color.r>.94f?1:.68f;var tint=new Color(brightness,brightness,brightness,color.a);var points=new[]{new Vector2(-w,-h),new Vector2(-w,h),new Vector2(w,h),new Vector2(w,-h)};
            var uvs=new[]{new Vector2(uv.xMin,uv.yMin),new Vector2(uv.xMin,uv.yMax),new Vector2(uv.xMax,uv.yMax),new Vector2(uv.xMax,uv.yMin)};
            for(int i=0;i<4;i++)vh.AddVert((Vector3)r.center+rotation*(Vector3)(points[i]*scale),tint,uvs[i]);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
