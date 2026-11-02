using UnityEngine;
using UnityEngine.Rendering;
namespace TalesTactics
{
    // Keep painted billboards facing the camera, but sort them from their feet.
    // The shader gives their pixels the depth of an upright world-space body.
    public static class StandingSpriteDepth
    {
        public static void Configure(SpriteRenderer renderer)
        {
            renderer.spriteSortPoint=SpriteSortPoint.Pivot;
            var properties=new MaterialPropertyBlock();renderer.GetPropertyBlock(properties);
            properties.SetFloat("_StandingDepth",1);renderer.SetPropertyBlock(properties);
        }
        public static void Configure(Camera camera)
        {
            // At a shared screen height, upright bodies order by horizontal foot
            // position. Ground height must not pull a rear sprite in front.
            var axis=camera.transform.forward;axis.y=0;
            camera.transparencySortMode=TransparencySortMode.CustomAxis;
            camera.transparencySortAxis=axis.sqrMagnitude>.0001f?axis.normalized:Vector3.forward;
        }
    }
}
