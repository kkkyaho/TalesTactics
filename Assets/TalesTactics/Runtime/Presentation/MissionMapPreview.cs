using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MissionMapPreview:UnityEngine.UI.MaskableGraphic
    {
        GridMap map;Vector2Int? goal;
        public void Initialize(int stage){map=CampaignContent.Map(stage);var kind=CampaignMissions.Kind(stage);goal=kind==ObjectiveKind.Reach||kind==ObjectiveKind.Escort?CampaignMissions.Destination(stage):(Vector2Int?)null;raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(map==null)return;var r=rectTransform.rect;
            int width=map.Tiles.Keys.Max(p=>p.x)+1,depth=map.Tiles.Keys.Max(p=>p.y)+1;
            float size=Mathf.Min(r.width/width,r.height/depth);
            Vector2 origin=r.center-new Vector2(width,depth)*size*.5f;
            foreach(var t in map.Tiles.Values)
            {
                var p=origin+(Vector2)t.Coordinate*size;Color tint=!t.Walkable?new Color(.20f,.26f,.31f):t.Terrain==TerrainType.Water?new Color(.12f,.39f,.49f):Color.Lerp(new Color(.30f,.41f,.46f),new Color(.51f,.60f,.61f),t.Height*.22f);
                if(t.Coordinate==goal)tint=new Color(.94f,.73f,.29f);
                Quad(vh,new Rect(p+Vector2.one*.65f,Vector2.one*(size-1.3f)),tint);
            }
        }
        static void Quad(UnityEngine.UI.VertexHelper vh,Rect r,Color color)
        {int n=vh.currentVertCount;vh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMin),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
