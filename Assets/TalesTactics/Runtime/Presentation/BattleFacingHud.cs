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
                var hit=g.GetComponent<UnityEngine.UI.Image>();hit.color=Color.clear;hit.raycastTarget=true;
                // Only the hit area is in the Canvas. The visible arrow follows the ground beneath sprites.
                var button=g.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=hit;button.transition=UnityEngine.UI.Selectable.Transition.None;
                button.onClick.AddListener(()=>battle.ChooseFacing(facing));
                var hover=g.AddComponent<FacingPointer>();hover.Battle=battle;hover.Direction=facing;
            }
            message.text="방향 선택 · Enter 확정 · Esc 취소";
            message.rectTransform.sizeDelta=new Vector2(-20,28);message.fontSizeMin=12;message.fontSizeMax=14;
            UpdateFacingArrows();
        }
        void UpdateFacingArrows()
        {
            var unit=battle.Session.Active;var camera=battle.Board.BattleCamera;if(unit==null||camera==null)return;
            float scale=GetComponent<Canvas>().scaleFactor;
            var world=battle.Session.Grid[unit.Position].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.03f;
            var foot=camera.WorldToScreenPoint(world);
            var center=new Vector2(foot.x/scale,foot.y/scale);
            var half=new Vector2(120,120);
            Place(commands,Vector2.zero,Vector2.zero,center-half,center+half);
            foreach(Transform child in commands)
            {
                if(!child.gameObject.activeSelf||!Enum.TryParse<Facing>(child.name,out var facing))continue;
                var direction=SkillResolver.FacingVector(facing);
                var dir=new Vector3(direction.x,0,direction.y);var side=Vector3.Cross(Vector3.up,dir);
                var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var max=-min;
                foreach(float distance in new[]{.42f,1.18f})foreach(float width in new[]{-.32f,.32f})
                {
                    var p=camera.WorldToScreenPoint(world+dir*distance+side*width)-foot;
                    var point=new Vector2(p.x,p.y)/scale;min=Vector2.Min(min,point);max=Vector2.Max(max,point);
                }
                var r=(RectTransform)child;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
                r.sizeDelta=Vector2.Max(max-min,new Vector2(32,24));r.anchoredPosition=(min+max)*.5f;
            }
        }
    }
}
