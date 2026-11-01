using UnityEngine;
namespace TalesTactics
{
    public sealed partial class PixelBattlefield
    {
        // Same footprint and ground-height rejection as Terrain.shader.
        public float GroundShadeAt(Vector3 point)
        {
            float shade=1;int count=Mathf.Min(24,shadowCasters.Count);
            for(int i=0;i<count;i++)
            {
                var c=shadowCasters[i];var delta=new Vector2(point.x-c.x-.20f*c.z,point.z-c.y-.15f*c.z);
                float contact=Mathf.Exp(-delta.sqrMagnitude/Mathf.Max(.001f,c.z*c.z*.22f));
                shade*=1-contact*.38f*Mathf.Clamp01(1-Mathf.Abs(point.y-c.w)*4);
            }
            return Mathf.Clamp(shade,.66f,1);
        }
    }
}
