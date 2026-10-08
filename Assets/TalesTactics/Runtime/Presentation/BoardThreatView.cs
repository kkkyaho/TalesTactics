using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        readonly Dictionary<Vector2Int,LineRenderer> threatMarks=new Dictionary<Vector2Int,LineRenderer>();
        readonly Dictionary<UnitRuntime,LineRenderer> actionMarks=new Dictionary<UnitRuntime,LineRenderer>();
        LineRenderer inspectMarker,hoverMarker,keyboardMarker,facingMarker;
        public void DrawThreats()
        {
            if(root==null||battle.Session==null)return;
            foreach(var m in threatMarks.Values)if(m!=null)m.enabled=false;
            var cells=new HashSet<Vector2Int>();var committed=new HashSet<Vector2Int>();
            foreach(var pair in battle.Threats)
            {
                if(pair.Key.IntentPhase==1)committed.UnionWith(pair.Value);
                if(battle.ThreatMode==2||battle.ThreatMode==1&&pair.Key==battle.InspectedUnit)cells.UnionWith(pair.Value);
            }
            cells.UnionWith(committed);
            foreach(var p in cells)
            {
                if(!threatMarks.TryGetValue(p,out var line)||line==null)
                {var g=new GameObject("Threat "+p);g.transform.SetParent(root,false);line=g.AddComponent<LineRenderer>();line.sharedMaterial=HighlightMaterial;threatMarks[p]=line;}
                bool fixedAttack=committed.Contains(p);var at=battle.Session.Grid[p].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.13f;
                line.enabled=true;line.loop=!fixedAttack;line.positionCount=fixedAttack?5:4;line.startWidth=line.endWidth=fixedAttack?.06f:.035f;
                var c=fixedAttack?new Color(1,.2f,.8f):new Color(1,.35f,.18f);line.startColor=line.endColor=c;SetColor(line,c);
                line.SetPositions(fixedAttack?new[]{at+new Vector3(-.34f,0,-.34f),at+new Vector3(.34f,0,.34f),at,at+new Vector3(-.34f,0,.34f),at+new Vector3(.34f,0,-.34f)}:
                    new[]{at+Vector3.forward*.3f,at+Vector3.right*.3f,at+Vector3.back*.3f,at+Vector3.left*.3f});
            }
        }
        void TacticalMarkers()
        {
            if(battle.Session.Scheduler is TeamTurnScheduler team)
            foreach(var ally in battle.Session.Units.Where(a=>a.Team==Team.Player))
            {
                actionMarks.TryGetValue(ally,out var mark);
                bool ready=team.Phase==Team.Player&&team.CanSelect(ally);
                var color=!ready?Color.gray:!team.HasBegun(ally)||!ally.Acted?new Color(.25f,.85f,.7f):new Color(.4f,.65f,1);
                Marker(ref mark,"Action availability "+ally.Data.Id,ally.Alive?ally.Position:(Vector2Int?)null,color,.28f);
                mark.loop=false;mark.positionCount=ready?3:2;
                var at=battle.Session.Grid[ally.Position].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.14f;
                mark.SetPositions(ready?new[]{at+Vector3.left*.22f,at+Vector3.forward*.2f,at+Vector3.right*.22f}:new[]{at+Vector3.left*.22f,at+Vector3.right*.22f});
                actionMarks[ally]=mark;
            }
            Marker(ref inspectMarker,"Inspected unit",battle.InspectedUnit?.Alive==true?battle.InspectedUnit.Position:(Vector2Int?)null,new Color(1,.8f,.1f),.42f);
            if(battle.InspectedUnit?.Alive==true)
            {
                var p=battle.Session.Grid[battle.InspectedUnit.Position].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.1f;
                inspectMarker.SetPositions(new[]{p+Vector3.forward*.52f,p+Vector3.right*.52f,p+Vector3.back*.52f,p+Vector3.left*.52f});
            }
            Marker(ref hoverMarker,"Turn order focus",battle.HoveredUnit?.Alive==true?battle.HoveredUnit.Position:(Vector2Int?)null,Color.cyan,.55f);
            Marker(ref keyboardMarker,"Keyboard tile cursor",battle.KeyboardTile,Color.cyan,.32f);
            var u=battle.InspectedUnit?.Alive==true?battle.InspectedUnit:battle.Session.Active;
            Marker(ref facingMarker,"Facing indicator",u?.Alive==true?u.Position:(Vector2Int?)null,Color.white,.2f);
            if(u?.Alive==true)
            {
                var origin=battle.Session.Grid[u.Position].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.17f;
                var d=SkillResolver.FacingVector(u.Facing);var forward=new Vector3(d.x,0,d.y);var right=new Vector3(-d.y,0,d.x);
                facingMarker.positionCount=3;facingMarker.SetPositions(new[]{origin+forward*.58f,origin+forward*.25f+right*.16f,origin+forward*.25f-right*.16f});
            }
        }
        void ResetTacticalMarkers(){actionMarks.Clear();threatMarks.Clear();inspectMarker=hoverMarker=keyboardMarker=facingMarker=null;}
    }
}
