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
                AddTacticalIcon(g.transform,"chevron",26,new Vector2(0,-10),Color.white);
                var icon=g.GetComponentInChildren<TacticalIcon>();
                icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=icon.rectTransform.pivot=new Vector2(.5f,.5f);icon.rectTransform.anchoredPosition=Vector2.zero;
                var outline=icon.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(0,0,0,.9f);outline.effectDistance=new Vector2(1,-1);
                var button=g.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=icon;
                var colors=button.colors;colors.normalColor=new Color(.92f,.94f,1);colors.highlightedColor=new Color(1,.78f,.3f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(1,.6f,.15f);button.colors=colors;
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
            var world=battle.Session.Grid[unit.Position].WorldPosition(battle.Catalog.Rules.TileHeight);
            var foot=camera.WorldToScreenPoint(world);
            var center=new Vector2(foot.x/scale,foot.y/scale);
            float radius=Mathf.Clamp(camera.WorldToScreenPoint(world+camera.transform.right*.85f).x-foot.x,44*scale,70*scale)/scale;
            var half=new Vector2(radius+20,radius+20);
            center.x=Mathf.Clamp(center.x,half.x+12,Screen.width/scale-half.x-12);
            center.y=Mathf.Clamp(center.y,half.y+12,Screen.height/scale-half.y-88);
            Place(commands,Vector2.zero,Vector2.zero,center-half,center+half);
            foreach(Transform child in commands)
            {
                if(!child.gameObject.activeSelf||!Enum.TryParse<Facing>(child.name,out var facing))continue;
                var direction=SkillResolver.FacingVector(facing);
                var projected=camera.WorldToScreenPoint(world+new Vector3(direction.x,0,direction.y))-foot;
                var offset=new Vector2(projected.x,projected.y).normalized*radius;
                var r=(RectTransform)child;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(40,40);r.anchoredPosition=offset;
                var icon=child.GetComponentInChildren<TacticalIcon>();
                icon.rectTransform.localEulerAngles=new Vector3(0,0,Mathf.Atan2(offset.y,offset.x)*Mathf.Rad2Deg);
                var button=child.GetComponent<UnityEngine.UI.Button>();var colors=button.colors;
                colors.normalColor=battle.PendingFacing==facing?new Color(.94f,.78f,.43f):new Color(.25f,.75f,.74f);button.colors=colors;
            }
        }
    }
}
