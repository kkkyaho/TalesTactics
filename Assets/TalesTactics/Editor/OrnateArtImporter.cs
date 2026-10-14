using UnityEditor;
using UnityEngine;
namespace TalesTactics.Editor
{
    public sealed class OrnateArtImporter:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Resources/TalesTactics/Ornate/"))return;
            var importer=(TextureImporter)assetImporter;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            if(assetPath.EndsWith("Panel.png")){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=new Vector4(170,170,170,170);importer.spritePixelsPerUnit=100;}
            else {importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Clamp;}
        }
    }
}
