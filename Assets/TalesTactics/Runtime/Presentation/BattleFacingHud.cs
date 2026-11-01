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
                var art=new GameObject("Ground direction pointer",typeof(RectTransform),typeof(CanvasRenderer),typeof(FacingArrowGraphic));art.transform.SetParent(g.transform,false);
                var r=(RectTransform)art.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(30,30);
                art.GetComponent<FacingArrowGraphic>().raycastTarget=false;
            }
            // Selecting a pointer is the confirmation. The optional tutorial still owns
            // its lesson text; normal combat has no second instruction panel.
            UpdateFacingArrows();
        }
        void UpdateFacingArrows()
        {
            var unit=battle.Session.Active;var camera=battle.Board.BattleCamera;if(unit==null||camera==null)return;
            float scale=Mathf.Max(.01f,GetComponent<Canvas>().scaleFactor),zoom=battle.Preferences?.TextScale??1;
            var ground=battle.Session.Grid[unit.Position].WorldPosition(battle.Catalog.Rules.TileHeight);
            var screen=camera.WorldToScreenPoint(ground);
            var center=new Vector2(screen.x/scale,screen.y/scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            var half=new Vector2(88,70)*zoom;
            center.x=Mathf.Clamp(center.x,half.x+8,w-half.x-8);center.y=Mathf.Clamp(center.y,half.y+8,h-half.y-8);
            Place(commands,Vector2.zero,Vector2.zero,center-half,center+half);
            var origin=camera.WorldToScreenPoint(ground);
            foreach(Transform child in commands)
            {
                if(!child.gameObject.activeSelf||!Enum.TryParse<Facing>(child.name,out var facing))continue;
                var d=SkillResolver.FacingVector(facing);var point=camera.WorldToScreenPoint(ground+new Vector3(d.x,0,d.y))-origin;
                var direction=new Vector2(point.x,point.y).normalized;
                // A shallow ground ellipse keeps the artwork at boot level, while the
                // invisible 44px targets remain easy to hit with mouse or controller.
                var r=(RectTransform)child;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(44,44)*zoom;
                r.anchoredPosition=new Vector2(direction.x*62,direction.y*44)*zoom;
                var arrow=child.GetComponentInChildren<FacingArrowGraphic>();arrow.rectTransform.sizeDelta=new Vector2(30,30)*zoom;
                arrow.SetDirection(direction);
                arrow.color=facing==battle.PendingFacing?new Color(.43f,.96f,.9f,1):new Color(.86f,.82f,.72f,1);
            }
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
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
            var points=new[]{new Vector2(-w,-h),new Vector2(-w,h),new Vector2(w,h),new Vector2(w,-h)};
            var uvs=new[]{new Vector2(uv.xMin,uv.yMin),new Vector2(uv.xMin,uv.yMax),new Vector2(uv.xMax,uv.yMax),new Vector2(uv.xMax,uv.yMin)};
            for(int i=0;i<4;i++)vh.AddVert((Vector3)r.center+rotation*(Vector3)(points[i]*scale),color,uvs[i]);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
