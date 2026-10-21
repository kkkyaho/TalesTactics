using UnityEngine;
namespace TalesTactics
{
    // Measured transparent islands, not equal cells: preserves full leaves, vines and props.
    public static class SdTerrainAtlas
    {
        static readonly Rect[] architecture = {
            new Rect(82f,972f,171f,179f),
            new Rect(401f,966f,146f,202f),
            new Rect(687f,972f,193f,160f),
            new Rect(1014f,964f,159f,200f),
            new Rect(74f,664f,191f,195f),
            new Rect(393f,668f,189f,142f),
            new Rect(711f,662f,150f,214f),
            new Rect(1010f,664f,166f,152f),
            new Rect(90f,366f,159f,198f),
            new Rect(431f,367f,120f,190f),
            new Rect(719f,371f,119f,207f),
            new Rect(1036f,377f,118f,182f),
            new Rect(73f,87f,214f,181f),
            new Rect(397f,92f,171f,133f),
            new Rect(728f,92f,119f,179f),
            new Rect(1011f,91f,161f,168f),
        };
        public static Rect Architecture(int index, Texture2D texture)
        {
            var r=architecture[index];
            return new Rect(r.x*texture.width/1254f,r.y*texture.height/1254f,r.width*texture.width/1254f,r.height*texture.height/1254f);
        }
        static readonly Rect[] foliage = {
            new Rect(97f,963f,166f,184f),
            new Rect(393f,974f,157f,168f),
            new Rect(691f,972f,173f,148f),
            new Rect(1011f,970f,145f,175f),
            new Rect(94f,639f,172f,154f),
            new Rect(384f,649f,184f,137f),
            new Rect(709f,612f,144f,215f),
            new Rect(1007f,641f,174f,163f),
            new Rect(88f,332f,168f,141f),
            new Rect(384f,319f,196f,168f),
            new Rect(701f,297f,171f,201f),
            new Rect(1026f,319f,151f,180f),
            new Rect(90f,60f,166f,165f),
            new Rect(407f,61f,141f,172f),
            new Rect(688f,63f,194f,142f),
            new Rect(1018f,63f,157f,177f),
        };
        public static Rect Foliage(int index, Texture2D texture)
        {
            var r=foliage[index];
            return new Rect(r.x*texture.width/1254f,r.y*texture.height/1254f,r.width*texture.width/1254f,r.height*texture.height/1254f);
        }
    }
}
