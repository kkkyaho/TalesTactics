using UnityEngine;
namespace TalesTactics
{
    // Update after authored motion without changing poses, grid positions or collision.
    [DefaultExecutionOrder(200)]
    public sealed class GroundedUnitVisual:MonoBehaviour
    {
        SpriteRenderer actor;GroundOverlay shadow;MeshRenderer surface;UnitRuntime unit;PixelBattlefield terrain;
        MaterialPropertyBlock properties;
        int theme;float bodyScale;
        public int Theme=>theme;
        public Vector3 GroundAnchor{get;private set;}
        public void Initialize(SpriteRenderer sprite,GroundOverlay contact,UnitRuntime owner,PixelBattlefield scenery,int chapter)
        {
            properties=new MaterialPropertyBlock();actor=sprite;shadow=contact;surface=contact.GetComponent<MeshRenderer>();unit=owner;terrain=scenery;theme=BattleEnvironmentPalette.Theme(chapter);
            bodyScale=Mathf.Clamp(CharacterVisualSizing.Scale(owner.Data),.65f,1.45f);
            shadow.Shadow(BattleEnvironmentPalette.Shadow(theme));
        }
        void LateUpdate()
        {
            if(actor==null||actor.sprite==null||shadow==null)return;
            var delta=actor.transform.position-transform.position;
            GroundAnchor=transform.position+new Vector3(delta.x,0,delta.z);
            float lift=Mathf.Max(0,delta.y-.06f);
            shadow.transform.position=GroundAnchor+Vector3.up*.018f;
            shadow.transform.localScale=Vector3.one*bodyScale*Mathf.Lerp(1,1.18f,Mathf.Clamp01(lift));
            shadow.gameObject.SetActive(unit.Alive);
            surface.GetPropertyBlock(properties);properties.SetColor("_Color",new Color(1,1,1,Mathf.Lerp(1,.45f,Mathf.Clamp01(lift*2))));surface.SetPropertyBlock(properties);
            properties.Clear();actor.GetPropertyBlock(properties);
            BattleEnvironmentPalette.ApplyUnit(properties,theme,GroundAnchor,actor.sprite.bounds,terrain!=null?terrain.GroundShadeAt(GroundAnchor):1);
            actor.SetPropertyBlock(properties);
        }
    }
}
