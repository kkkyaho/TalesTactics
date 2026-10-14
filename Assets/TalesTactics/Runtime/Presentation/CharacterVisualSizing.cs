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
            var original=Character.Sprites?.Front??sprite;
            float bodyWorld=original.rect.height/original.pixelsPerUnit*CharacterVisualSizing.BodyFraction(Character);
            float bodyHeight=Mathf.Min(rect.height*.8f,rect.width/1.2f);
            float scale=bodyHeight/bodyWorld;
            // Use the imported silhouette mesh, retaining transparent gaps around bows and spears.
            // Body scale uses the original art so a longer signature weapon cannot shrink the hero.
            var vertices=sprite.vertices;var uv=sprite.uv;var triangles=sprite.triangles;
            float bottom=rect.yMin+sprite.pivot.y/sprite.pixelsPerUnit*scale;
            for(int i=0;i<vertices.Length;i++)mesh.AddVert(new Vector3(rect.center.x+vertices[i].x*scale,bottom+vertices[i].y*scale),color,uv[i]);
            for(int i=0;i<triangles.Length;i+=3)mesh.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
        }
    }
}
