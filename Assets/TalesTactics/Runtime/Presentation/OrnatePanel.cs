using UnityEngine;

namespace TalesTactics
{
    // The painted laurel corners stay constant as windows resize. Content keeps its established safe inset.
    [DisallowMultipleComponent]
    public sealed class OrnatePanel:MonoBehaviour
    {
        UnityEngine.UI.Image image;
        float requestedCorner=40;
        public static void Apply(UnityEngine.UI.Image target,float corner=40)
        {
            var sprite=SdUiTheme.PanelArt;
            if(sprite==null)sprite=Resources.Load<Sprite>("TalesTactics/Ornate/Panel");
            if(sprite==null)return;
            target.sprite=sprite;target.type=UnityEngine.UI.Image.Type.Sliced;
            target.pixelsPerUnitMultiplier=SdUiTheme.SlicePixels(sprite)/Mathf.Max(4,corner);
            target.color=new Color(1,1,1,target.color.a);
            var component=target.GetComponent<OrnatePanel>();if(component==null)component=target.gameObject.AddComponent<OrnatePanel>();component.image=target;component.requestedCorner=corner;
            if(target.GetComponent<FramedContent>()==null)target.gameObject.AddComponent<FramedContent>();
            var outline=target.GetComponent<UnityEngine.UI.Outline>();if(outline!=null)outline.effectColor=Color.clear;
        }
        void LateUpdate()
        {
            if(image==null)image=GetComponent<UnityEngine.UI.Image>();
            if(image==null)return;
            image.pixelsPerUnitMultiplier=SdUiTheme.SlicePixels(image.sprite)/Mathf.Max(4,Mathf.Min(requestedCorner,image.rectTransform.rect.height*.20f));
            // Existing screens own alpha/visibility. Do not multiply the painted navy and brass by old flat tints.
            if(image.color!=new Color(1,1,1,image.color.a))image.color=new Color(1,1,1,image.color.a);
        }
    }
}
