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
                AddTacticalIcon(g.transform,"chevron",20,new Vector2(0,-10),Color.white);
                var icon=g.GetComponentInChildren<TacticalIcon>();
                icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=icon.rectTransform.pivot=new Vector2(.5f,.5f);icon.rectTransform.anchoredPosition=Vector2.zero;
                var outline=icon.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(0,0,0,.9f);outline.effectDistance=new Vector2(1,-1);
                var button=g.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=icon;
                var colors=button.colors;colors.normalColor=new Color(.92f,.94f,1);colors.highlightedColor=new Color(1,.78f,.3f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(1,.6f,.15f);button.colors=colors;
                button.onClick.AddListener(()=>battle.ChooseFacing(facing));
            }
            UpdateFacingArrows();
        }
        void UpdateFacingArrows()
        {
            var unit=battle.Session.Active;var camera=battle.Board.BattleCamera;if(unit==null||camera==null)return;
            float scale=GetComponent<Canvas>().scaleFactor;
            var world=battle.Session.Grid[unit.Position].WorldPosition(battle.Catalog.Rules.TileHeight);
            var foot=camera.WorldToScreenPoint(world);var head=camera.WorldToScreenPoint(world+camera.transform.up*1.4f);
            var center=new Vector2((foot.x+head.x)*.5f/scale,(foot.y+head.y)*.5f/scale);
            float bodyHeight=Mathf.Abs(head.y-foot.y)/scale;
            float rx=Mathf.Max(40,bodyHeight*.35f+18),ry=bodyHeight*.5f+22;
            var half=new Vector2(rx+20,ry+20);
            center.x=Mathf.Clamp(center.x,half.x+12,Screen.width/scale-half.x-12);
            center.y=Mathf.Clamp(center.y,half.y+12,Screen.height/scale-half.y-88);
            Place(commands,Vector2.zero,Vector2.zero,center-half,center+half);
            foreach(Transform child in commands)
            {
                if(!child.gameObject.activeSelf||!Enum.TryParse<Facing>(child.name,out var facing))continue;
                var view=CharacterMotion.ViewFacing(facing,camera.transform.rotation);
                var offset=view==Facing.Front?new Vector2(0,-ry):view==Facing.Back?new Vector2(0,ry):view==Facing.Left?new Vector2(-rx,0):new Vector2(rx,0);
                var r=(RectTransform)child;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(40,40);r.anchoredPosition=offset;
                var icon=child.GetComponentInChildren<TacticalIcon>();
                icon.rectTransform.localEulerAngles=new Vector3(0,0,view==Facing.Front?-90:view==Facing.Back?90:view==Facing.Left?180:0);
                var button=child.GetComponent<UnityEngine.UI.Button>();var colors=button.colors;
                colors.normalColor=unit.Facing==facing?new Color(1,.78f,.3f):new Color(.92f,.94f,1);button.colors=colors;
            }
        }
    }
}
