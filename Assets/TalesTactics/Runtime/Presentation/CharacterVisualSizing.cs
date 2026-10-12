using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;
namespace TalesTactics
{
    // Optical body measurements of the authored front poses. Weapons do not set body height.
    public static class CharacterVisualSizing
    {
        public const float BodyHeight=1.22f;
        public static float BodyFraction(CharacterData c)=>c.Id=="jade"?.82f:c.Id=="mint"?.96f:1f;
        public static bool IsHero(CharacterData c)=>c!=null&&(c.Id=="cless"||c.Id=="mint"||c.Id=="farah"||c.Id=="jade"||c.Id=="tear"||c.Id=="natalia"||c.Id=="velvet"||c.Id=="alphen"||c.Id=="shionne"||c.Id=="kisara");
        public static float Scale(CharacterData c)
        {
            var front=c?.Sprites?.Front;
            return !IsHero(c)||front==null?1:BodyHeight/(front.rect.height/front.pixelsPerUnit*BodyFraction(c));
        }
    }
    // Keeps the foot pivot, body height and weapon extent independent of the cell's aspect ratio.
    public sealed class CharacterModelImage:UnityEngine.UI.Image
    {
        public CharacterData Character;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            if(sprite==null||Character==null){base.OnPopulateMesh(mesh);return;}
            mesh.Clear();var rect=GetPixelAdjustedRect();
            float bodyPixels=sprite.rect.height*CharacterVisualSizing.BodyFraction(Character);
            float bodyHeight=Mathf.Min(rect.height*.8f,rect.width/1.2f);
            float scale=bodyHeight/bodyPixels;
            float width=sprite.rect.width*scale,height=sprite.rect.height*scale;
            float left=rect.center.x-sprite.pivot.x*scale,bottom=rect.yMin;
            var uv=DataUtility.GetOuterUV(sprite);
            mesh.AddVert(new Vector3(left,bottom),color,new Vector2(uv.x,uv.y));
            mesh.AddVert(new Vector3(left,bottom+height),color,new Vector2(uv.x,uv.w));
            mesh.AddVert(new Vector3(left+width,bottom+height),color,new Vector2(uv.z,uv.w));
            mesh.AddVert(new Vector3(left+width,bottom),color,new Vector2(uv.z,uv.y));
            mesh.AddTriangle(0,1,2);mesh.AddTriangle(2,3,0);
        }
    }
}
