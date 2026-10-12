using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        GroundOverlay threatArea,intentArea,inspectMarker,hoverMarker,keyboardMarker;
        Material unitToneMaterial;
        MaterialPropertyBlock toneProperties;
        static readonly Vector2Int[] edgeDirections={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        Material UnitToneMaterial=>unitToneMaterial!=null?unitToneMaterial:unitToneMaterial=new Material(Resources.Load<Shader>("TalesTactics/UnitTone")){name="Runtime unit tone"};
        GroundOverlay Overlay(string name,Transform parent,int order=-2)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);var overlay=g.AddComponent<GroundOverlay>();overlay.Initialize(UnitToneMaterial,order);return overlay;}
        public void DrawThreats()
        {
            if(root==null||battle.Session==null)return;
            var cells=new HashSet<Vector2Int>();var committed=new HashSet<Vector2Int>();
            foreach(var pair in battle.Threats)
            {
                if(!pair.Key.Alive)continue;
                if(pair.Key.IntentPhase==1)committed.UnionWith(pair.Value);
                if(battle.ThreatMode==2||battle.ThreatMode==1&&pair.Key==battle.InspectedUnit)cells.UnionWith(pair.Value);
            }
            cells.ExceptWith(committed);
            Region(ref threatArea,"Threat region",cells,new Color(.69f,.29f,.23f,.10f),new Color(.77f,.38f,.3f,.62f));
            // Committed attacks stay visible with threat display off, without a cross on every tile.
            Region(ref intentArea,"Committed attack region",committed,new Color(.85f,.46f,.19f,.18f),new Color(.98f,.64f,.3f,.86f));
        }
        void Region(ref GroundOverlay overlay,string name,HashSet<Vector2Int> cells,Color fill,Color outline)
        {
            if(overlay==null)overlay=Overlay(name,root,-4);
            overlay.Clear();overlay.enabled=cells.Count>0;
            foreach(var p in cells)
            {
                var tile=battle.Session.Grid[p];if(tile==null)continue;
                var at=tile.WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.012f;
                overlay.Tile(at,.5f,fill);
                foreach(var d in edgeDirections)
                {
                    var neighbor=battle.Session.Grid[p+d];
                    if(cells.Contains(p+d)&&neighbor!=null&&neighbor.Height==tile.Height)continue;
                    var normal=new Vector3(d.x,0,d.y);var along=new Vector3(-d.y,0,d.x)*.5f;
                    overlay.Line(at+normal*.49f-along,at+normal*.49f+along,.016f,outline);
                }
            }
            overlay.Apply();
        }
        void TacticalMarkers()
        {
            if(toneProperties==null)toneProperties=new MaterialPropertyBlock();
            var team=battle.Session.Scheduler as TeamTurnScheduler;
            foreach(var pair in sprites)
            {
                var unit=pair.Key;
                bool done=unit.Alive&&unit.Team==Team.Player&&team!=null&&(team.Phase!=Team.Player||!team.CanSelect(unit));
                pair.Value.GetPropertyBlock(toneProperties);
                toneProperties.SetFloat("_Saturation",done?.38f:1);toneProperties.SetFloat("_Brightness",done?.84f:1);
                pair.Value.SetPropertyBlock(toneProperties);
            }
            bool visible=!battle.StoryActive&&!battle.Hud.InputModalOpen;
            var inspected=battle.InspectedUnit;var hovered=battle.HoveredUnit;
            Marker(ref inspectMarker,"Inspected unit",visible&&inspected?.Alive==true&&inspected!=battle.Session.Active&&inspected.Position!=battle.Target?inspected.Position:(Vector2Int?)null,new Color(.79f,.67f,.43f,.7f),.43f);
            Marker(ref hoverMarker,"Turn order focus",visible&&hovered?.Alive==true&&hovered!=battle.Session.Active&&hovered!=inspected?hovered.Position:(Vector2Int?)null,new Color(.39f,.69f,.72f,.6f),.43f);
            Marker(ref keyboardMarker,"Keyboard tile cursor",visible&&battle.KeyboardTile!=battle.Target&&battle.KeyboardTile!=battle.Session.Active?.Position?battle.KeyboardTile:null,new Color(.39f,.69f,.72f,.6f),.43f);
        }
        void ResetTacticalMarkers(){threatArea=intentArea=inspectMarker=hoverMarker=keyboardMarker=null;if(unitToneMaterial!=null)Destroy(unitToneMaterial);unitToneMaterial=null;}
        void OnDestroy(){if(unitToneMaterial!=null)Destroy(unitToneMaterial);}
    }
}
