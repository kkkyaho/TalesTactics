using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        static readonly Color PathColor=new Color(.36f,.88f,.86f);
        SpriteRenderer pathArrow;
        SpriteRenderer destinationGhost;
        readonly List<LineRenderer> movementEdges=new List<LineRenderer>();
        Vector2Int? previewDestination;
        void ResetMovementPreview(){pathArrow=null;destinationGhost=null;previewDestination=null;movementEdges.Clear();}
        public void ShowMovementRange(IEnumerable<Vector2Int> points)
        {
            var set=new HashSet<Vector2Int>(points);ShowRange(set,new Color(.15f,.55f,.62f));
            int index=0;
            foreach(var p in set)
            {
                var center=battle.Session.Grid[p].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.032f;
                foreach(var d in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})
                {
                    if(set.Contains(p+d))continue;
                    var normal=new Vector3(d.x,0,d.y);var along=new Vector3(-d.y,0,d.x)*.48f;
                    if(index==movementEdges.Count)movementEdges.Add(ThinLine("Movement boundary",root,PathColor,.025f));
                    var edge=movementEdges[index++];edge.enabled=true;edge.positionCount=2;
                    edge.SetPositions(new[]{center+normal*.48f-along,center+normal*.48f+along});
                }
            }
        }
        void ClearMovementPreview()
        {previewDestination=null;if(pathArrow!=null)pathArrow.enabled=false;if(destinationGhost!=null)destinationGhost.enabled=false;}
        LineRenderer ThinLine(string name,Transform parent,Color color,float width,bool world=true)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);var l=g.AddComponent<LineRenderer>();
            l.sharedMaterial=HighlightMaterial;l.useWorldSpace=world;l.startWidth=l.endWidth=width;
            l.startColor=l.endColor=color;l.numCornerVertices=2;l.numCapVertices=2;
            l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;l.receiveShadows=false;SetColor(l,color);return l;
        }
        void CreateContactShadow(Transform parent)
        {
            var shadow=Overlay("Contact shadow",parent,-3);shadow.transform.localPosition=Vector3.up*.018f;shadow.Shadow();
        }
        void DrawMovementPreview(List<Vector2Int> path)
        {
            ClearMovementPreview();line.positionCount=path.Count>1?path.Count:0;
            bool valid=path.Count>1;
            Color tint=valid&&battle.DangerAt(path[path.Count-1])>0?new Color(.93f,.68f,.32f):PathColor;
            Marker(ref destinationMarker,"Movement destination",valid?path[path.Count-1]:(Vector2Int?)null,tint,.40f);
            if(!valid)return;
            line.startColor=line.endColor=tint;SetColor(line,tint);
            for(int i=0;i<path.Count;i++)line.SetPosition(i,battle.Session.Grid[path[i]].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.045f);
            if(pathArrow==null){var g=new GameObject("Path arrowhead");g.transform.SetParent(root,false);pathArrow=g.AddComponent<SpriteRenderer>();pathArrow.sprite=IllustratedIcons.Sprite("compact-direction");pathArrow.sharedMaterial=SpriteMaterial;pathArrow.sortingOrder=6;}
            pathArrow.enabled=true;pathArrow.color=Color.white;
            var end=line.GetPosition(path.Count-1);var direction=(end-line.GetPosition(path.Count-2)).normalized;
            pathArrow.transform.SetPositionAndRotation(end+Vector3.up*.01f,Quaternion.LookRotation(Vector3.up,direction));pathArrow.transform.localScale=Vector3.one*.32f;
            if(destinationGhost==null)
            {var g=new GameObject("Destination ghost");g.transform.SetParent(root,false);destinationGhost=g.AddComponent<SpriteRenderer>();destinationGhost.sharedMaterial=SpriteMaterial;destinationGhost.sortingOrder=5;}
            destinationGhost.enabled=true;destinationGhost.color=new Color(.55f,1,1,.65f);
            previewDestination=path[path.Count-1];UpdateMovementPreview();
        }
        void UpdateMovementPreview()
        {
            if(!previewDestination.HasValue||destinationGhost==null)return;
            var unit=battle.Session.Active;
            destinationGhost.sprite=EquipmentAppearance.Resolve(unit,unit.Data.Sprites.Get(CharacterMotion.ViewFacing(unit.Facing,BattleCamera.transform.rotation)));
            destinationGhost.transform.SetPositionAndRotation(battle.Session.Grid[previewDestination.Value].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.04f,BattleCamera.transform.rotation);
            destinationGhost.transform.localScale=Vector3.one*CharacterVisualSizing.Scale(unit.Data);
        }
    }
}
