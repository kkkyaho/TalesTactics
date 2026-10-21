using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        // Source rectangles use top-left atlas coordinates. Keep each object isolated:
        // the rifle stock crosses the nominal grid boundary by a few pixels.
        static readonly Rect[] classArtRegions =
        {
            new Rect(44,20,385,410),
            new Rect(516,14,379,419),
            new Rect(924,44,374,404),
            new Rect(1361,18,394,430),
            new Rect(38,445,366,414),
            new Rect(455,444,439,411),
            new Rect(877,459,445,399),
            new Rect(1400,441,339,415)
        };

        void ClassArt(Transform parent,CharacterData character,Vector2 min,Vector2 max,Color? tint=null)
        {
            int cell;
            switch(character.Id)
            {
                case "mint":cell=1;break;
                case "farah":cell=2;break;
                case "jade":cell=3;break;
                case "tear":cell=4;break;
                case "natalia":cell=5;break;
                case "shionne":cell=6;break;
                case "kisara":cell=7;break;
                default:cell=0;break;
            }
            var bounds=FormationRect(parent,"ClassIcon-"+character.Id,min,max);
            var art=FormationRect(bounds,"ClassIllustration",Vector2.zero,Vector2.one);
            var image=art.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            image.texture=Resources.Load<Texture2D>("TalesTactics/Ornate/ClassIcons");
            var region=classArtRegions[cell];
            image.uvRect=new Rect(region.x/1774f,1-(region.y+region.height)/887f,region.width/1774f,region.height/887f);
            image.color=tint??Color.white;image.raycastTarget=false;SdUiIconArt.Apply(image);
            var fit=art.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio=region.width/region.height;
        }
    }
}
