using UnityEngine;
using UnityEngine.UI;

namespace TalesTactics
{
    // Uses the imported nine-slice sprite: corners stay constant as windows resize.
    [DisallowMultipleComponent]
    public sealed class OrnatePanel:MonoBehaviour
    {
        Image image;
        float requestedCorner=40;
        public static void Apply(Image target,float corner=40)
        {
            var sprite=Resources.Load<Sprite>("TalesTactics/Ornate/Panel");
            if(sprite==null)return;
            target.sprite=sprite;target.type=Image.Type.Sliced;target.pixelsPerUnitMultiplier=170/corner;
            target.color=new Color(1,1,1,target.color.a);
            var component=target.GetComponent<OrnatePanel>();if(component==null)component=target.gameObject.AddComponent<OrnatePanel>();component.image=target;component.requestedCorner=corner;
            var outline=target.GetComponent<Outline>();if(outline!=null)outline.effectColor=Color.clear;
        }
        void LateUpdate()
        {
            if(image==null)image=GetComponent<Image>();
            if(image!=null)image.pixelsPerUnitMultiplier=170/Mathf.Max(4,Mathf.Min(requestedCorner,image.rectTransform.rect.height*.20f));
            // Existing screens continue to own visibility/alpha; their old flat tint must not darken the metal.
            if(image!=null&&image.color!=new Color(1,1,1,image.color.a))image.color=new Color(1,1,1,image.color.a);
        }
    }
}
