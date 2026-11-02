using UnityEngine;
namespace TalesTactics
{
    // Chapter lighting is shared by terrain, props and actors; status tone is separate.
    public static class BattleEnvironmentPalette
    {
        // Shared world light: camera rotation must not rotate the sun or its ground shadows.
        public static readonly Vector3 KeyDirection=new Vector3(-.45f,.85f,-.35f).normalized;
        public static readonly Vector3 ShadowDirection=new Vector3(.45f,0,.35f).normalized;
        public static int Theme(int chapter)=>chapter>=CampaignStages.LegacyCount?CrossoverCampaign.TerrainTheme(chapter):Mathf.Clamp(chapter,0,5);
        public static bool Verdant(int theme)=>theme==0||theme==1||theme==4;
        public static Color Key(int theme)=>theme==2?new Color(1.04f,.97f,.86f):theme==3||theme==5?new Color(.94f,.99f,1.04f):new Color(1.02f,1,.91f);
        public static Color Fill(int theme)=>theme==2?new Color(.86f,.78f,.73f):theme==3||theme==5?new Color(.75f,.84f,.96f):new Color(.78f,.87f,.85f);
        public static Color Shadow(int theme)=>theme==2?new Color(.15f,.12f,.13f):new Color(.09f,.14f,.16f);
        public static void ApplyTerrain(Material material,int theme)
        {
            material.SetColor("_KeyColor",Key(theme));material.SetColor("_FillColor",Fill(theme));
            material.SetVector("_KeyDirection",KeyDirection);
            material.SetFloat("_ArtSaturation",Verdant(theme)?.78f:.88f);
        }
        public static void ApplyScenery(Material material,int theme)
        {
            material.SetFloat("_ArtSaturation",Verdant(theme)?.72f:.86f);
            material.SetFloat("_ArtContrast",.91f);material.SetFloat("_Brightness",.97f);
            material.SetColor("_KeyColor",Key(theme));material.SetColor("_FillColor",Fill(theme));
            material.SetFloat("_EnvironmentLight",.72f);material.SetFloat("_CanopyShade",Verdant(theme)?.12f:.025f);
            material.SetVector("_SpriteSpan",new Vector4(0,1,0,0));
        }
        public static void ApplyUnit(MaterialPropertyBlock block,int theme,Vector3 foot,Bounds bounds,float shade)
        {
            block.SetFloat("_ArtSaturation",.92f);block.SetFloat("_ArtContrast",.97f);
            block.SetColor("_EnvironmentTint",Color.white);
            block.SetColor("_KeyColor",Key(theme));block.SetColor("_FillColor",Fill(theme));
            block.SetFloat("_EnvironmentLight",.78f);block.SetFloat("_CanopyShade",Verdant(theme)?.085f:.018f);
            block.SetVector("_KeyDirection",KeyDirection);block.SetFloat("_BodyVolume",.065f);
            block.SetFloat("_GroundShade",shade);block.SetVector("_GroundAnchor",new Vector4(foot.x,foot.y,foot.z,1));
            block.SetVector("_SpriteSpan",new Vector4(bounds.min.y,Mathf.Max(.01f,bounds.size.y),bounds.min.x,Mathf.Max(.01f,bounds.size.x)));
        }
        public static void ApplyPanorama(Material material,int theme)
        {
            material.SetColor("_Atmosphere",theme==2?new Color(.30f,.27f,.24f):theme==3||theme==5?new Color(.18f,.24f,.33f):new Color(.22f,.29f,.27f));
        }
    }
}
