using TMPro;
using UnityEngine;

namespace TalesTactics
{
    // Shared painted surfaces and ink colours; layout, gameplay and accessibility sizing stay authored.
    public static class SdUiTheme
    {
        public static readonly Color Ink = new Color32(6, 14, 25, 255);
        public static readonly Color Navy = new Color32(13, 29, 48, 255);
        public static readonly Color Button = new Color32(20, 39, 59, 255);
        public static readonly Color Quiet = new Color32(15, 31, 48, 255);
        public static readonly Color Selected = new Color32(27, 79, 87, 255);
        public static readonly Color Primary = new Color32(29, 81, 89, 255);
        public static readonly Color Disabled = new Color32(21, 32, 46, 255);
        public static readonly Color Text = new Color32(246, 244, 232, 255);
        public static readonly Color Muted = new Color32(147, 160, 172, 255);
        public static readonly Color Brass = new Color32(216, 183, 117, 255);
        public static readonly Color Teal = new Color32(121, 220, 212, 255);
        static Sprite panelArt;
        public static Sprite PanelArt
        {
            get
            {
                if(panelArt==null)panelArt=Resources.Load<Sprite>("TalesTactics/Ornate/SdPanel");
                return panelArt;
            }
        }
        public static float SlicePixels(Sprite sprite)=>sprite!=null?Mathf.Max(1,sprite.border.x):170;
        public static Color Surface(HudRole role,bool active,bool disabled)
            =>disabled?Disabled:active?Selected:role==HudRole.Primary?Primary:role==HudRole.Tab||role==HudRole.Quiet?Quiet:Button;
        public static void StyleText(TMP_Text text)
        {
            if(text==null)return;
            float size=text.enableAutoSizing?text.fontSizeMax:text.fontSize;
            var button=text.GetComponentInParent<UnityEngine.UI.Button>();
            bool buttonTitle=button!=null&&button.GetComponentInChildren<TMP_Text>()==text&&size>=16;
            if(buttonTitle||size>=24)text.fontStyle|=FontStyles.Bold;
        }
    }
}
