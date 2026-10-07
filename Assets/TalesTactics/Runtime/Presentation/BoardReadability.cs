using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        bool focusAction;
        LineRenderer activeMarker,targetMarker,destinationMarker;
        public void FocusCurrent()
        {if(root==null||battle.TimingActive)return;focusAction=true;zoom=1;FitBattlefield(true);}
        public void RefreshFocus(){if(focusAction&&root!=null)FitBattlefield(true);}
        void FocusBounds(ref Vector2 min,ref Vector2 max)
        {
            var p=BattleCamera.transform.InverseTransformPoint(battle.Session.Grid[battle.Session.Active.Position].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.6f);
            var a=new Vector2(p.x,p.y);var b=a;
            if(battle.Target.HasValue)
            {p=BattleCamera.transform.InverseTransformPoint(battle.Session.Grid[battle.Target.Value].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.6f);b=new Vector2(p.x,p.y);}
            var low=Vector2.Min(a,b)-new Vector2(3.2f,2.8f);var high=Vector2.Max(a,b)+new Vector2(3.2f,2.8f);
            if(high.y-low.y<max.y-min.y&&high.x-low.x<max.x-min.x){min=low;max=high;}
        }
        void UpdateSelectionMarkers()
        {
            TacticalMarkers();
            var u=battle.Session.Active;
            Marker(ref activeMarker,"Active unit",u!=null&&u.Alive?u.Position:(Vector2Int?)null,Color.white,.48f);
            Marker(ref targetMarker,"Selected target",battle.Target,new Color(1,.82f,.12f),.37f);
        }
        void Marker(ref LineRenderer marker,string name,Vector2Int? position,Color color,float radius)
        {
            if(marker==null)
            {
                var g=new GameObject(name);g.transform.SetParent(root,false);marker=g.AddComponent<LineRenderer>();
                marker.sharedMaterial=HighlightMaterial;marker.loop=true;marker.positionCount=4;marker.startWidth=marker.endWidth=.07f;marker.startColor=marker.endColor=color;SetColor(marker,color);
            }
            marker.enabled=position.HasValue;if(!position.HasValue)return;
            marker.positionCount=4;
            marker.startColor=marker.endColor=color;SetColor(marker,color);
            var p=battle.Session.Grid[position.Value].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.08f;
            marker.SetPositions(new[]{p+new Vector3(-radius,0,-radius),p+new Vector3(radius,0,-radius),p+new Vector3(radius,0,radius),p+new Vector3(-radius,0,radius)});
        }
    }
}
