using UnityEngine;
namespace TalesTactics
{
    public sealed partial class PixelBattlefield
    {
        // The existing tile cube still owns collision and its exact top height. These faces only
        // expose the real rise with a wall material instead of wrapping pavement down the side.
        void DressHeightBoundary(GridTile tile,GridTile neighbor,Vector3 outward,float step)
        {
            float rise=(tile.Height-neighbor.Height)*step;
            if(rise<=0)return;
            var top=tile.WorldPosition(step);
            var tangent=Vector3.Cross(outward,Vector3.up);
            var edge=top+outward*.503f;
            var left=edge-tangent*.499f;var right=edge+tangent*.499f;
            int surface=stage==2?6:wall;
            geometry[surface].Quad(left-Vector3.up*rise,left,right,right-Vector3.up*rise,.88f,new Vector2(1,rise));

            // Small separated capstones stay on the rim, leaving the standing/picking center clear.
            float noise=DetailNoise(tile.Coordinate.x,tile.Coordinate.y,53+(int)outward.x*3+(int)outward.z*7);
            for(int i=0;i<3;i++)
            {
                float thickness=.035f+noise*.018f;
                var center=top+outward*.458f+tangent*((i-1)*.325f)+Vector3.up*thickness*.25f;
                geometry[surface].Box(center,new Vector3(outward.x==0?.305f:.086f,thickness,outward.z==0?.305f:.086f),.93f+noise*.05f);
            }

            // A one-level walkable join gets shallow visible treads. Larger drops and water keep
            // their cliff silhouette; decorative stairs must not imply a new traversable route.
            if(tile.Height-neighbor.Height!=1||!tile.Walkable||!neighbor.Walkable||neighbor.Terrain==TerrainType.Water)return;
            for(int i=0;i<3;i++)
            {
                float height=rise*(3-i)/4f;
                var center=top+outward*(.555f+i*.12f)-Vector3.up*(rise-height*.5f);
                geometry[surface].Box(center,new Vector3(outward.x==0?.96f:.13f,height,outward.z==0?.96f:.13f),.94f);
            }
        }
    }
}
