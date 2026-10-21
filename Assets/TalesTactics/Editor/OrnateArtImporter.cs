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
            if(assetPath.EndsWith("Panel.png"))
            {
                // The SD frame has a wider authored corner; the legacy frame keeps its own slice.
                float border=assetPath.EndsWith("/SdPanel.png")?240:170;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spriteBorder=Vector4.one*border;importer.spritePixelsPerUnit=100;
            }
            else {importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Clamp;}
        }
    }
}
