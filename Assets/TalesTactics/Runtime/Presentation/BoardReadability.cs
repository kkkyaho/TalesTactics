using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        bool focusAction;
        GroundOverlay activeMarker,targetMarker,destinationMarker;
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
            bool visible=!battle.StoryActive&&!battle.Hud.InputModalOpen;
            Marker(ref activeMarker,"Active unit",visible&&u!=null&&u.Alive&&!(battle.State is FacingSelectionState)?u.Position:(Vector2Int?)null,new Color(.35f,.78f,.78f,.86f),.43f);
            Marker(ref targetMarker,"Selected target",visible?battle.Target:null,new Color(.86f,.69f,.37f,.8f),.43f);
        }
        void Marker(ref GroundOverlay marker,string name,Vector2Int? position,Color color,float radius)
        {
            if(marker==null)marker=Overlay(name,root);
            marker.enabled=position.HasValue;if(!position.HasValue)return;
            marker.Corners(radius,color);
            marker.transform.position=battle.Session.Grid[position.Value].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.022f;
        }
    }
}
