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
                var hit=g.GetComponent<UnityEngine.UI.Image>();hit.color=new Color(.04f,.08f,.12f,.6f);hit.raycastTarget=true;
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
                arrow.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);
                bool selected=facing==battle.PendingFacing;
                arrow.color=selected?new Color(1,.82f,.48f):new Color(.81f,.9f,.92f);
                child.GetComponent<UnityEngine.UI.Image>().color=selected?new Color(.25f,.22f,.15f,.92f):new Color(.025f,.045f,.07f,.78f);
            }
        }
    }
    public sealed class FacingArrowGraphic:UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Vector2[] p={new Vector2(0,1),new Vector2(-1,0),new Vector2(-.38f,0),new Vector2(-.38f,-1),new Vector2(.38f,-1),new Vector2(.38f,0),new Vector2(1,0)};
            foreach(var v in p)vh.AddVert(new Vector3(r.center.x+v.x*r.width*.5f,r.center.y+v.y*r.height*.5f),color,Vector2.zero);
            vh.AddTriangle(0,1,6);vh.AddTriangle(2,3,4);vh.AddTriangle(2,4,5);
        }
    }
}
